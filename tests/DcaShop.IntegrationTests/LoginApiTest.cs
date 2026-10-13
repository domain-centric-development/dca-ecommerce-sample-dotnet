using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Logging in and out through the API: a login hands out a token for the account, a wrong password is refused,
/// and a logout is the client discarding its token.
/// </summary>
/// <remarks>
/// Each test starts its own shop: the accounts live in memory, and every test registers the same address.
/// </remarks>
public sealed class LoginApiTest : IDisposable
{
    private const string Email = "ada@example.com";
    private const string Password = "Secret123";

    private readonly WebApplicationFactory<Program> _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task LoggingInAnswersOkWithATokenAndTheAddress()
    {
        await RegisterAsync();

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", Login(Email, Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
        Assert.Equal(Email, body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task AWrongPasswordIsUnauthorized()
    {
        await RegisterAsync();

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", Login(Email, "Wrong1234"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoggingOutAnswersNoContentWithAnEmptyBody()
    {
        await RegisterAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LogInAsync());

        var response = await client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
    }

    private async Task RegisterAsync()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            email = Email,
            password = Password,
            firstName = "Ada",
            lastName = "Lovelace",
            dateOfBirth = "1815-12-10",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<string> LogInAsync()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", Login(Email, Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private static object Login(string email, string password) => new { email, password };
}