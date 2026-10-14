
using System.Net;
using System.Text.RegularExpressions;

using DcaShop.Account.Application.RegisterAccount;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The registration page through the real HTTP pipeline, each test as a visitor with no cookies: how the header leads
/// there, what the form asks for, and where a registration and the login link lead when the page was opened for
/// another page.
/// </summary>
public sealed class RegistrationPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RegistrationPageTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheHeaderOfTheCatalogueOffersARegisterLinkToTheRegistrationPage()
    {
        var client = Visitor();

        var catalogue = await client.GetStringAsync("/products");

        var register = Link(Element(catalogue, "site-header", "</header>"), "nav-register-link");
        Assert.Equal("Register", register.Text);
        Assert.Equal("/register", register.Target);
    }

    [Fact]
    public async Task TheRegistrationPageAsksForOwnerAddressAndPasswordWithItsHintsAndLimits()
    {
        var client = Visitor();

        var page = await client.GetStringAsync("/register");

        Assert.Equal("Create Account", Text(page, @"class=""auth__title"">([^<]*)</h2>"));
        var form = Element(page, "register-form", "</form>");
        Assert.Equal(
            ["First Name", "Last Name", "Date of Birth", "Email", "Password", "Confirm Password"],
            Regex.Matches(form, @"<label\b[^>]*>([^<]*)</label>").Select(m => Rendered(m.Groups[1].Value)));
        foreach (Match label in Regex.Matches(form, @"<label\b[^>]*for=""([^""]+)"""))
        {
            Assert.Matches($@"<input\b[^>]*id=""{Regex.Escape(label.Groups[1].Value)}""", form);
        }
        Assert.Equal("Create Account", Text(form, @"data-test=""register-submit-button"">([^<]*)</button>"));
        var hints = Regex.Matches(form, @"class=""form__hint"">([^<]*)</p>").Select(m => Rendered(m.Groups[1].Value)).ToList();
        Assert.Contains("Your name cannot be changed later", hints);
        Assert.Contains("Password must be at least 8 characters", hints);
        Assert.Equal("100", MaxLength(form, "firstName"));
        Assert.Equal("100", MaxLength(form, "lastName"));
        Assert.Equal("72", MaxLength(form, "password"));
        Assert.Equal("72", MaxLength(form, "confirmPassword"));
        var login = Link(page, "register-login-link");
        Assert.Equal("Login here", login.Text);
        Assert.Equal("/login", login.Target);
    }

    [Fact]
    public async Task ARegistrationOpenedForTheCatalogueReturnsThereLoggedIn()
    {
        var client = Visitor();
        var page = await client.GetStringAsync("/register?returnUrl=/products");

        var registered = await client.PostAsync(
            "/register",
            Forms.AsRendered(
                Element(page, "register-form", "</form>"),
                ("firstName", "Anna"),
                ("lastName", "Schmidt"),
                ("dateOfBirth", "1990-05-17"),
                ("email", "anna.schmidt@example.com"),
                ("password", "Secret123"),
                ("confirmPassword", "Secret123")));

        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);
        Assert.Equal("/products", registered.Headers.Location!.ToString());
        var catalogue = await client.GetStringAsync(registered.Headers.Location);
        Assert.Equal(
            "Welcome, anna.schmidt@example.com",
            Text(Element(catalogue, "site-header", "</header>"), @"data-test=""user-greeting"">([^<]*)</span>"));
    }

    [Fact]
    public async Task ARegistrationOpenedForTheCatalogueSaysThereOnceThatTheAccountWasCreated()
    {
        using var shop = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(s => s.AddSingleton<IRegisterAccountInputPort>(new SucceedingRegistration())));
        var client = Visitor(shop);
        var page = await client.GetStringAsync("/register?returnUrl=/products");

        var registered = await client.PostAsync(
            "/register",
            Forms.AsRendered(
                Element(page, "register-form", "</form>"),
                ("firstName", "Anna"),
                ("lastName", "Schmidt"),
                ("dateOfBirth", "1990-05-17"),
                ("email", "anna.schmidt@example.com"),
                ("password", "Secret123"),
                ("confirmPassword", "Secret123")));

        Assert.Equal("/products", registered.Headers.Location!.ToString());
        var catalogue = await client.GetStringAsync(registered.Headers.Location);
        Assert.Equal(
            ["Account created successfully! Welcome!"],
            Regex.Matches(catalogue, @"data-test=""account-notice"">([^<]*)</div>").Select(m => Rendered(m.Groups[1].Value)));
    }

    [Fact]
    public async Task TheLoginLinkOfARegistrationOpenedForTheCatalogueKeepsTheCatalogue()
    {
        var client = Visitor();

        var page = await client.GetStringAsync("/register?returnUrl=/products");

        var login = Link(page, "register-login-link");
        Assert.Equal("/login?returnUrl=/products", Uri.UnescapeDataString(login.Target));
    }

    [Fact]
    public async Task APasswordShorterThanEightCharactersIsRefused()
    {
        var refusal = await RefusalOfAsync("anna.short@example.com", ("password", "Sec123"), ("confirmPassword", "Sec123"));

        Assert.Equal("Password must be at least 8 characters", refusal);
    }

    [Fact]
    public async Task ADateOfBirthThatIsNotADateIsRefused()
    {
        var refusal = await RefusalOfAsync("anna.unreadable@example.com", ("dateOfBirth", "17.05.1990"));

        Assert.Equal("Please enter a date like 1990-05-17", refusal);
    }

    [Fact]
    public async Task ADateOfBirthInTheFutureIsRefused()
    {
        var refusal = await RefusalOfAsync("anna.future@example.com", ("dateOfBirth", "2999-01-01"));

        Assert.Equal("Please check the email address, your name and your date of birth.", refusal);
    }

    [Fact]
    public async Task AnAddressWithoutATopLevelDomainIsRefused()
    {
        var refusal = await RefusalOfAsync("anna.schmidt@example");

        Assert.Equal("Please check the email address, your name and your date of birth.", refusal);
    }

    [Fact]
    public async Task ABlankFirstNameIsRefused()
    {
        var refusal = await RefusalOfAsync("anna.blank@example.com", ("firstName", "   "));

        Assert.Equal("Please check the email address, your name and your date of birth.", refusal);
    }

    [Fact]
    public async Task ALastNameOfMoreThanAHundredCharactersIsRefused()
    {
        var refusal = await RefusalOfAsync("anna.overlong@example.com", ("lastName", new string('S', 101)));

        Assert.Equal("Please check the email address, your name and your date of birth.", refusal);
    }

    [Fact]
    public async Task AnAddressAnotherAccountHasIsRefusedWhateverItsCapitalsAndSurroundingSpaces()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(Visitor(shop), "anna.schmidt@example.com")).StatusCode);

        var refused = await RegisterAsync(Visitor(shop), " Anna.Schmidt@Example.COM ");

        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        Assert.Equal(
            "This email address is already registered",
            Text(await refused.Content.ReadAsStringAsync(), @"data-test=""register-error-message"">([^<]*)</div>"));
    }

    [Fact]
    public async Task ABrowserThatAlreadyHasAnAccountCannotRegisterASecondOneAndStaysLoggedInToTheFirst()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        var client = Visitor(shop);
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, "anna.schmidt@example.com")).StatusCode);

        var refused = await RegisterAsync(client, "anna.second@example.com");

        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        var page = await refused.Content.ReadAsStringAsync();
        Assert.Equal("You already have an account.", Text(page, @"data-test=""register-error-message"">([^<]*)</div>"));
        Assert.Equal(
            "Welcome, anna.schmidt@example.com",
            Text(Element(page, "site-header", "</header>"), @"data-test=""user-greeting"">([^<]*)</span>"));
    }

    /// <summary>
    /// Submits an otherwise valid registration for <paramref name="email"/>, with <paramref name="changed"/> in place
    /// of the valid values, as a visitor with no cookies; returns the refusal the registration page shows.
    /// </summary>
    private async Task<string> RefusalOfAsync(string email, params (string Name, string Value)[] changed)
    {
        var client = Visitor();
        var page = await client.GetStringAsync("/register");
        var valid = new Dictionary<string, string>
        {
            ["firstName"] = "Anna",
            ["lastName"] = "Schmidt",
            ["dateOfBirth"] = "1990-05-17",
            ["email"] = email,
            ["password"] = "Secret123",
            ["confirmPassword"] = "Secret123",
        };
        foreach (var (name, value) in changed)
        {
            valid[name] = value;
        }

        var answer = await client.PostAsync(
            "/register",
            Forms.AsRendered(Element(page, "register-form", "</form>"), [.. valid.Select(f => (f.Key, f.Value))]));

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        var refused = await answer.Content.ReadAsStringAsync();
        Assert.Equal("Create Account", Text(refused, @"class=""auth__title"">([^<]*)</h2>"));
        return Text(refused, @"data-test=""register-error-message"">([^<]*)</div>");
    }

    /// <summary>A browser of its own, with no cookies yet; redirects are not followed — each one is an assertion.</summary>
    private HttpClient Visitor() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>A browser of its own in <paramref name="shop"/>, a shop with a store of its own.</summary>
    private static HttpClient Visitor(WebApplicationFactory<Program> shop) =>
        shop.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Opens the registration page and submits it with Anna's details under <paramref name="email"/>.</summary>
    private static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email)
    {
        var page = await client.GetStringAsync("/register");
        return await client.PostAsync(
            "/register",
            Forms.AsRendered(
                Element(page, "register-form", "</form>"),
                ("firstName", "Anna"),
                ("lastName", "Schmidt"),
                ("dateOfBirth", "1990-05-17"),
                ("email", email),
                ("password", "Secret123"),
                ("confirmPassword", "Secret123")));
    }

    /// <summary>A registration that accepts every command as a new customer — the page's translation alone.</summary>
    private sealed class SucceedingRegistration : IRegisterAccountInputPort
    {
        public Task<RegisterAccountResult> ExecuteAsync(RegisterAccountCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RegisterAccountResult(
                $"account-{command.CurrentUserId}", command.CurrentUserId, command.Email, new HashSet<Role> { Role.Customer }));
    }

    /// <summary>The <c>maxlength</c> of the input named <paramref name="name"/>.</summary>
    private static string MaxLength(string form, string name)
    {
        var input = Regex.Match(form, $@"<input\b[^>]*name=""{Regex.Escape(name)}""[^>]*>");
        Assert.True(input.Success, $"the form has an input {name}");
        return Regex.Match(input.Value, @"maxlength=""(\d+)""").Groups[1].Value;
    }

    /// <summary>The markup from the element carrying <paramref name="dataTest"/> to its closing tag.</summary>
    private static string Element(string page, string dataTest, string closingTag)
    {
        var start = page.IndexOf($"data-test=\"{dataTest}\"", StringComparison.Ordinal);
        Assert.True(start >= 0, $"the page has a {dataTest}");
        return page[start..].Split(closingTag)[0];
    }

    /// <summary>The target and text of the anchor carrying <paramref name="dataTest"/>.</summary>
    private static (string Target, string Text) Link(string markup, string dataTest)
    {
        var match = Regex.Match(markup, $@"<a\b[^>]*href=""([^""]+)""[^>]*data-test=""{Regex.Escape(dataTest)}"">([^<]*)</a>");
        Assert.True(match.Success, $"the markup offers a {dataTest}");
        return (WebUtility.HtmlDecode(match.Groups[1].Value), Rendered(match.Groups[2].Value));
    }

    private static string Text(string markup, string pattern)
    {
        var match = Regex.Match(markup, pattern);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return Rendered(match.Groups[1].Value);
    }

    /// <summary>The text a reader sees: tags removed, entities decoded.</summary>
    private static string Rendered(string html) => WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", string.Empty)).Trim();
}