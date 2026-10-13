using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// "My Profile" through the real HTTP pipeline: what it shows of an account, what saving it changes, which address
/// logs in afterwards, and how a visitor without a session is sent to the login. Each test that registers Anna does
/// so in a shop with a store of its own.
/// </summary>
public sealed class ProfilePageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Email = "anna.schmidt@example.com";
    private const string NewEmail = "anna.meyer@example.com";
    private const string Password = "Secret123";

    private readonly WebApplicationFactory<Program> _factory;

    public ProfilePageTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheAccountNavigationLeadsToTheFixedNameAndTheAddressAndDateOfBirthThatCanChange()
    {
        var client = Visitor(_factory.WithWebHostBuilder(_ => { }));
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);
        var overview = await client.GetStringAsync("/account");

        var myProfile = Link(Element(overview, "account-nav", "</nav>"), "account-nav-profile");
        var response = await client.GetAsync(myProfile.Target);

        var profile = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/account/profile", myProfile.Target);
        Assert.Equal("My Profile", Text(profile, @"data-test=""profile-title"">([^<]*)</h2>"));
        Assert.Equal("Basic Information", Text(profile, @"<h3 class=""card__header-title"">([^<]*)</h3>"));
        Assert.Equal(
            "My Profile",
            Text(Element(profile, "account-nav", "</nav>"), @"<a\b[^>]*aria-current=""page""[^>]*>([^<]*)</a>"));
        Assert.Equal("Anna Schmidt", Text(profile, @"data-test=""profile-owner-name"">([^<]*)</p>"));
        Assert.Equal(
            "Your name was set when you created your account and cannot be changed.",
            Text(profile, @"data-test=""profile-name-note"">([^<]*)</p>"));
        Assert.DoesNotMatch(@"<input\b[^>]*name=""(firstName|lastName|name|fullName)""", profile);
        Assert.Equal(Email, InputValue(profile, "profile-email-input"));
        Assert.Equal(
            "This address is also your login",
            Text(profile, @"data-test=""profile-email-input"">\s*<p class=""form__hint"">([^<]*)</p>"));
        Assert.Equal("1990-05-17", InputValue(profile, "profile-date-of-birth-input"));
        Assert.Equal("Save Changes", Text(profile, @"data-test=""profile-submit-button"">([^<]*)</button>"));
    }

    [Fact]
    public async Task TheOwnAddressInOtherCapitalsIsSavedAsTheSameAddress()
    {
        var client = Visitor(_factory.WithWebHostBuilder(_ => { }));
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);

        var saved = await SaveProfileAsync(client, "Anna.Schmidt@Example.com", "1990-05-17");

        var profile = await saved.Response.Content.ReadAsStringAsync();
        Assert.Equal("/account/profile", saved.Path);
        Assert.Equal("Your profile has been updated.", Text(profile, @"data-test=""profile-success-message"">([^<]*)</div>"));
        Assert.Equal(Email, InputValue(profile, "profile-email-input"));
    }

    [Fact]
    public async Task TheNewAddressLogsInFromABrowserWithNoCookies()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        await ChangeAddressAsync(shop);
        var client = Visitor(shop);

        var loggedIn = await LoginAsync(client, NewEmail);

        var landing = await FollowRedirectsAsync(client, loggedIn);
        Assert.Equal("/cart", landing.Path);
        Assert.Equal(
            $"Welcome, {NewEmail}",
            Text(await landing.Response.Content.ReadAsStringAsync(), @"data-test=""user-greeting"">([^<]*)</span>"));
    }

    [Fact]
    public async Task TheOldAddressNoLongerLogsIn()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        await ChangeAddressAsync(shop);
        var client = Visitor(shop);

        var refused = await LoginAsync(client, Email);

        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        Assert.Equal(
            "Invalid email or password",
            Text(await refused.Content.ReadAsStringAsync(), @"data-test=""login-error-message"">([^<]*)</div>"));
    }

    [Fact]
    public async Task AVisitorWithoutCookiesIsSentFromTheProfileToTheLogin()
    {
        var client = Visitor(_factory);

        var response = await client.GetAsync("/account/profile");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?returnUrl=%2Faccount%2Fprofile", response.Headers.Location!.ToString());
    }

    /// <summary>
    /// A browser of its own in <paramref name="shop"/>, with no cookies yet; it keeps the cookies it is given, and
    /// redirects are not followed — each one is an assertion.
    /// </summary>
    private static HttpClient Visitor(WebApplicationFactory<Program> shop) =>
        shop.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Opens the registration page and submits it with Anna's details under <paramref name="email"/>.</summary>
    private static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email)
    {
        var page = await client.GetStringAsync("/register");
        return await client.PostAsync(
            "/register",
            Submission(
                Element(page, "register-form", "</form>"),
                ("firstName", "Anna"),
                ("lastName", "Schmidt"),
                ("dateOfBirth", "1990-05-17"),
                ("email", email),
                ("password", Password),
                ("confirmPassword", Password)));
    }

    /// <summary>Registers Anna in <paramref name="shop"/> and moves her address to the new one, in a browser of its own.</summary>
    private static async Task ChangeAddressAsync(WebApplicationFactory<Program> shop)
    {
        var client = Visitor(shop);
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);
        var saved = await SaveProfileAsync(client, NewEmail, "1990-05-17");
        Assert.Equal("/account/profile", saved.Path);
    }

    /// <summary>Opens the profile, submits it with the given details and follows the answer as a browser would.</summary>
    private static async Task<(string Path, HttpResponseMessage Response)> SaveProfileAsync(
        HttpClient client, string email, string dateOfBirth)
    {
        var page = await client.GetStringAsync("/account/profile");
        var answer = await client.PostAsync(
            "/account/profile",
            Submission(Element(page, "profile-form", "</form>"), ("email", email), ("dateOfBirth", dateOfBirth)));
        return await FollowRedirectsAsync(client, answer);
    }

    /// <summary>Opens the login page and submits it with <paramref name="email"/> and Anna's password.</summary>
    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email)
    {
        var page = await client.GetStringAsync("/login");
        return await client.PostAsync(
            "/login", Submission(Element(page, "login-form", "</form>"), ("email", email), ("password", Password)));
    }

    /// <summary>Follows the redirects of <paramref name="response"/> as a browser would; answers where it ends.</summary>
    private static async Task<(string Path, HttpResponseMessage Response)> FollowRedirectsAsync(
        HttpClient client, HttpResponseMessage response)
    {
        var path = string.Empty;
        for (var hops = 0; response.StatusCode == HttpStatusCode.Redirect && hops < 5; hops++)
        {
            path = response.Headers.Location!.ToString();
            response = await client.GetAsync(path);
        }

        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
        return (path, response);
    }

    /// <summary>The form as the browser would send it: its hidden fields as rendered, plus what the visitor typed.</summary>
    private static FormUrlEncodedContent Submission(string form, params (string Name, string Value)[] typed)
    {
        var hidden = Regex.Matches(form, @"<input\b[^>]*type=""hidden""[^>]*>")
            .Select(input => (
                Name: Regex.Match(input.Value, @"name=""([^""]+)""").Groups[1].Value,
                Value: WebUtility.HtmlDecode(Regex.Match(input.Value, @"value=""([^""]*)""").Groups[1].Value)));

        return new FormUrlEncodedContent(
            hidden.Concat(typed).Select(f => new KeyValuePair<string, string>(f.Name, f.Value)));
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

    /// <summary>The value the input carrying <paramref name="dataTest"/> is rendered with.</summary>
    private static string InputValue(string page, string dataTest) =>
        Text(page, $@"<input\b[^>]*\bvalue=""([^""]*)""[^>]*data-test=""{Regex.Escape(dataTest)}""");

    private static string Text(string markup, string pattern)
    {
        var match = Regex.Match(markup, pattern);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return Rendered(match.Groups[1].Value);
    }

    /// <summary>The text a reader sees: tags removed, entities decoded.</summary>
    private static string Rendered(string html) => WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", string.Empty)).Trim();
}