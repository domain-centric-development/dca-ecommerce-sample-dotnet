using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The account's forms posted without the token their page carries, through the real HTTP pipeline: the shop refuses
/// them as forbidden and does nothing they asked for.
/// </summary>
public sealed class FormTokenRefusalTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Secret123";

    private readonly WebApplicationFactory<Program> _factory;

    public FormTokenRefusalTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ARegistrationPostedWithoutTheFormTokenIsForbiddenAndCreatesNoAccount()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        var client = Visitor(shop);
        await client.GetStringAsync("/register");

        var refused = await client.PostAsync(
            "/register",
            Fields(
                ("firstName", "Grace"),
                ("lastName", "Hopper"),
                ("dateOfBirth", "1990-05-17"),
                ("email", "grace@example.com"),
                ("password", Password),
                ("confirmPassword", Password)));

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        var fresh = Visitor(shop);
        var loginPage = await fresh.GetStringAsync("/login");
        var login = await fresh.PostAsync(
            "/login", Submission(loginPage, ("email", "grace@example.com"), ("password", Password)));
        Assert.Equal(
            "Invalid email or password",
            Text(await login.Content.ReadAsStringAsync(), @"data-test=""login-error-message"">([^<]*)</div>"));
    }

    [Fact]
    public async Task ALogoutPostedWithoutTheFormTokenIsForbiddenAndKeepsTheCustomerLoggedIn()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        var client = Visitor(shop);
        var registration = await client.GetStringAsync("/register");
        var registered = await client.PostAsync(
            "/register",
            Submission(
                registration,
                ("firstName", "Ada"),
                ("lastName", "Lovelace"),
                ("dateOfBirth", "1990-05-17"),
                ("email", "ada@example.com"),
                ("password", Password),
                ("confirmPassword", Password)));
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);

        var refused = await client.PostAsync("/logout", Fields());

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        var home = await client.GetStringAsync("/");
        Assert.Equal("Welcome, ada@example.com", Text(home, @"data-test=""user-greeting"">([^<]*)</span>"));
    }

    /// <summary>
    /// A browser of its own in <paramref name="shop"/>, with no cookies yet; it keeps the cookies it is given, and
    /// redirects are not followed — each one is an assertion.
    /// </summary>
    private static HttpClient Visitor(WebApplicationFactory<Program> shop) =>
        shop.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>The fields alone, as a page elsewhere would post them: no token of the shop's form.</summary>
    private static FormUrlEncodedContent Fields(params (string Name, string Value)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Name, f.Value)));

    /// <summary>The fields plus the antiforgery token the rendered page carries, as the shop's own form sends them.</summary>
    private static FormUrlEncodedContent Submission(string renderedPage, params (string Name, string Value)[] fields)
    {
        var token = Regex
            .Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""")
            .Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token), "the page carries an antiforgery token");
        return Fields([.. fields, ("__RequestVerificationToken", token)]);
    }

    private static string Text(string markup, string pattern)
    {
        var match = Regex.Match(markup, pattern);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
    }
}