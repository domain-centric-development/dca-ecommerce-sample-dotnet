using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The change-password page through the real HTTP pipeline: the form it shows, a change to the same password, which
/// password logs in afterwards, and how a visitor without a session is sent to the login. Each test that registers
/// Anna does so in a shop with a store of its own.
/// </summary>
public sealed class ChangePasswordPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Email = "anna.schmidt@example.com";
    private const string Password = "Secret123";
    private const string NewPassword = "Better456";
    private const string ChangePasswordPath = "/account/change-password";

    private static readonly string[] PasswordFields = ["currentPassword", "newPassword", "confirmPassword"];

    private readonly WebApplicationFactory<Program> _factory;

    public ChangePasswordPageTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheAccountNavigationLeadsToAnEmptyFormForTheCurrentPasswordAndTheNewOneTwice()
    {
        var client = Visitor(_factory.WithWebHostBuilder(_ => { }));
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);
        var overview = await client.GetStringAsync("/account");
        var target = Regex.Match(
            Element(overview, "account-nav", "</nav>"),
            @"<a\b[^>]*href=""([^""]+)""[^>]*data-test=""account-nav-change-password"">Change Password</a>");
        Assert.True(target.Success, "the account navigation offers Change Password");

        var page = await client.GetStringAsync(WebUtility.HtmlDecode(target.Groups[1].Value));

        Assert.Equal(ChangePasswordPath, WebUtility.HtmlDecode(target.Groups[1].Value));
        Assert.Equal("Change Password", Text(page, @"data-test=""change-password-title"">([^<]*)</h2>"));
        Assert.Equal(
            "Change Password",
            Text(Element(page, "account-nav", "</nav>"), @"<a\b[^>]*aria-current=""page""[^>]*>([^<]*)</a>"));

        var form = Element(page, "change-password-form", "</form>");
        Assert.Equal(
            ["Current Password", "New Password", "Confirm New Password"],
            PasswordFields.Select(field => Label(form, field)));
        Assert.All(
            PasswordFields,
            field => Assert.DoesNotContain("value=", Input(form, field), StringComparison.Ordinal));
        Assert.Equal("Change Password", Text(form, @"data-test=""change-password-submit-button"">([^<]*)</button>"));
        Assert.Equal(
            "Password must be at least 8 characters and contain an uppercase letter, a lowercase letter and a digit",
            Text(form, @"name=""newPassword""[^>]*>\s*<p class=""form__hint"">([^<]*)</p>"));
        Assert.All(
            ["newPassword", "confirmPassword"],
            field => Assert.Contains(@"maxlength=""72""", Input(form, field), StringComparison.Ordinal));
    }

    [Fact]
    public async Task KeepingTheCurrentPasswordAsTheNewOneIsAChangeLikeAnyOther()
    {
        var client = Visitor(_factory.WithWebHostBuilder(_ => { }));
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);

        var changed = await ChangePasswordAsync(client, Password, Password);

        var answer = await FollowRedirectsAsync(client, changed);
        Assert.Equal(
            "Your password has been changed.",
            Text(await answer.Response.Content.ReadAsStringAsync(), @"data-test=""change-password-success-message"">([^<]*)</div>"));
    }

    [Fact]
    public async Task TheNewPasswordLogsInFromABrowserWithNoCookies()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        await ChangedPasswordAsync(shop);
        var client = Visitor(shop);
        var page = await client.GetStringAsync("/login");

        var loggedIn = await client.PostAsync(
            "/login", Submission(Element(page, "login-form", "</form>"), ("email", Email), ("password", NewPassword)));

        var landing = await FollowRedirectsAsync(client, loggedIn);
        Assert.Equal("/cart", landing.Path);
        Assert.Equal(
            $"Welcome, {Email}",
            Text(await landing.Response.Content.ReadAsStringAsync(), @"data-test=""user-greeting"">([^<]*)</span>"));
    }

    [Fact]
    public async Task TheOldPasswordNoLongerLogsIn()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        await ChangedPasswordAsync(shop);
        var client = Visitor(shop);
        var page = await client.GetStringAsync("/login");

        var refused = await client.PostAsync(
            "/login", Submission(Element(page, "login-form", "</form>"), ("email", Email), ("password", Password)));

        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        Assert.Equal(
            "Invalid email or password",
            Text(await refused.Content.ReadAsStringAsync(), @"data-test=""login-error-message"">([^<]*)</div>"));
    }

    [Fact]
    public async Task AVisitorWithoutCookiesIsSentFromTheChangePasswordPageToTheLogin()
    {
        var client = Visitor(_factory);

        var response = await client.GetAsync(ChangePasswordPath);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?returnUrl=%2Faccount%2Fchange-password", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task AConfirmationThatDiffersIsNamedEvenWhenTheCurrentPasswordIsWrong()
    {
        var client = Visitor(_factory.WithWebHostBuilder(_ => { }));
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);

        var refused = await SubmitChangeAsync(client, "Wrong999", NewPassword, "Better457");

        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        Assert.Equal("New password and confirmation do not match", ErrorMessage(await refused.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task AWrongCurrentPasswordIsRefusedWithItsOwnSentence()
    {
        var client = Visitor(_factory.WithWebHostBuilder(_ => { }));
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);

        var refused = await SubmitChangeAsync(client, "Wrong999", NewPassword, NewPassword);

        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        Assert.Equal("Current password is not correct", ErrorMessage(await refused.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task AfterARefusedChangeTheOldPasswordStillLogsIn()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        var customer = Visitor(shop);
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(customer, Email)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SubmitChangeAsync(customer, "Wrong999", NewPassword, NewPassword)).StatusCode);
        var client = Visitor(shop);
        var page = await client.GetStringAsync("/login");

        var loggedIn = await client.PostAsync(
            "/login", Submission(Element(page, "login-form", "</form>"), ("email", Email), ("password", Password)));

        var landing = await FollowRedirectsAsync(client, loggedIn);
        Assert.Equal("/cart", landing.Path);
        Assert.Equal(
            $"Welcome, {Email}",
            Text(await landing.Response.Content.ReadAsStringAsync(), @"data-test=""user-greeting"">([^<]*)</span>"));
    }

    /// <summary>Opens the change-password page and submits the three fields as typed.</summary>
    private static async Task<HttpResponseMessage> SubmitChangeAsync(
        HttpClient client, string current, string replacement, string confirmation)
    {
        var page = await client.GetStringAsync(ChangePasswordPath);
        return await client.PostAsync(
            ChangePasswordPath,
            Submission(
                Element(page, "change-password-form", "</form>"),
                ("currentPassword", current),
                ("newPassword", replacement),
                ("confirmPassword", confirmation)));
    }

    private static string ErrorMessage(string page) =>
        Text(page, @"data-test=""change-password-error-message"">([^<]*)</div>");

    /// <summary>Registers Anna in <paramref name="shop"/> and changes her password from the old one to the new one.</summary>
    private static async Task ChangedPasswordAsync(WebApplicationFactory<Program> shop)
    {
        var client = Visitor(shop);
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);
        var changed = await ChangePasswordAsync(client, Password, NewPassword);
        Assert.Equal(ChangePasswordPath, changed.Headers.Location!.ToString());
    }

    /// <summary>Opens the change-password page and submits it with the new password typed twice.</summary>
    private static async Task<HttpResponseMessage> ChangePasswordAsync(HttpClient client, string current, string replacement)
    {
        var page = await client.GetStringAsync(ChangePasswordPath);
        return await client.PostAsync(
            ChangePasswordPath,
            Submission(
                Element(page, "change-password-form", "</form>"),
                ("currentPassword", current),
                ("newPassword", replacement),
                ("confirmPassword", replacement)));
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

    /// <summary>The input named <paramref name="name"/>, its whole tag.</summary>
    private static string Input(string form, string name)
    {
        var match = Regex.Match(form, $@"<input\b[^>]*name=""{Regex.Escape(name)}""[^>]*>");
        Assert.True(match.Success, $"the form has an input {name}");
        return match.Value;
    }

    /// <summary>The text of the label that belongs to the input named <paramref name="name"/>.</summary>
    private static string Label(string form, string name)
    {
        var id = Regex.Match(Input(form, name), @"\bid=""([^""]+)""").Groups[1].Value;
        return Text(form, $@"<label\b[^>]*for=""{Regex.Escape(id)}""[^>]*>([^<]*)</label>");
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