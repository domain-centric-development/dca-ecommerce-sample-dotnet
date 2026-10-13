using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The API trusts the <c>Authorization: Bearer</c> header and nothing else (ADR-007): the pages' session cookies open
/// nothing on it, it hands out no cookie of its own, and a token the shop did not sign is no token at all.
/// </summary>
/// <remarks>
/// Each test starts its own shop: the accounts live in memory, and two tests register the same address.
/// </remarks>
public sealed class TokenOnlyApiTest : IDisposable
{
    private const string SessionCookie = "shop-session";

    private readonly WebApplicationFactory<Program> _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ThePagesSessionCookiesDoNotAuthenticateAnApiCall()
    {
        var cookies = new CookieContainer();
        var browser = _factory.CreateDefaultClient(new CookieContainerHandler(cookies));
        var registerPage = await browser.GetStringAsync("/register");
        await browser.PostAsync(
            "/register",
            Form(
                registerPage,
                ("email", "ada@example.com"),
                ("password", "Secret123"),
                ("confirmPassword", "Secret123"),
                ("firstName", "Ada"),
                ("lastName", "Lovelace"),
                ("dateOfBirth", "1815-12-10")));
        Assert.NotNull(cookies.GetCookies(new Uri("http://localhost"))[SessionCookie]);

        var response = await browser.GetAsync("/api/carts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task RegisteringThroughTheApiHandsOutNoCookie()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", Registration("ada@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task ATokenWithAnAlteredSignatureIsChallengedLikeNoToken()
    {
        var registered = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", Registration("ada@example.com"));
        var token = (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", WithOneSignatureCharacterChanged(token));

        var response = await client.PostAsync("/api/products", NewProduct());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    /// <summary>
    /// The first character of the signature, swapped for another: all six of its bits count, so the signature no
    /// longer matches whatever the payload says.
    /// </summary>
    private static string WithOneSignatureCharacterChanged(string jwt)
    {
        var signatureStart = jwt.LastIndexOf('.') + 1;
        var replacement = jwt[signatureStart] == 'A' ? 'B' : 'A';
        return string.Concat(jwt.AsSpan(0, signatureStart), replacement.ToString(), jwt.AsSpan(signatureStart + 1));
    }

    private static object Registration(string email) => new
    {
        email,
        password = "Secret123",
        firstName = "Ada",
        lastName = "Lovelace",
        dateOfBirth = "1815-12-10",
    };

    private static StringContent NewProduct() => new(
        JsonSerializer.Serialize(new
        {
            sku = "TOKEN-001",
            name = "Token Product",
            description = "Created with a token the shop did not sign",
            imageUrl = "",
            price = 19.99m,
            category = "Books",
            stock = 5,
        }),
        Encoding.UTF8,
        "application/json");

    private static FormUrlEncodedContent Form(string renderedPage, params (string Name, string Value)[] fields)
    {
        var token = Regex
            .Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""")
            .Groups[1].Value;

        return new FormUrlEncodedContent(
            fields.Append(("__RequestVerificationToken", token))
                .Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}