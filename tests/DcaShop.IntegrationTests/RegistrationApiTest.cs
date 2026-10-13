using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Registering through the API: the success hands out a token, every refusal is a problem document whose title
/// names the failure and whose detail is the adapter's own sentence.
/// </summary>
/// <remarks>
/// Each test starts its own shop: the accounts live in memory, and several tests begin with or without the same
/// address.
/// </remarks>
public sealed class RegistrationApiTest : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task RegisteringAnswersCreatedWithATokenAndTheAddress()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", Registration("ada@example.com", "Secret123", "Ada", "Lovelace", "1815-12-10"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
        Assert.Equal("ada@example.com", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task AnAddressAnotherAccountHoldsIsAConflictNamingTheAddress()
    {
        await RegisterAsync("ada@example.com");

        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", Registration("ada@example.com", "Secret123", "Grace", "Hopper", "1906-12-09"));

        var problem = await ProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("Email already registered", problem.GetProperty("title").GetString());
        Assert.Equal("The email address ada@example.com is already registered.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task APasswordWithoutAnUppercaseLetterIsRefusedNamingTheRule()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", Registration("grace@example.com", "secret123", "Grace", "Hopper", "1906-12-09"));

        var problem = await ProblemAsync(response, HttpStatusCode.UnprocessableEntity);
        Assert.Equal("Password too weak", problem.GetProperty("title").GetString());
        Assert.Equal("Password must contain an uppercase letter.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AMalformedAddressIsABadRequestWithoutDetail()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", Registration("grace@example", "Secret123", "Grace", "Hopper", "1906-12-09"));

        var problem = await ProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task ACallerWhoAlreadyHasAnAccountCannotRegisterAnother()
    {
        var token = await RegisterAsync("ada@example.com");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(
            "/api/auth/register", Registration("grace@example.com", "Secret123", "Grace", "Hopper", "1906-12-09"));

        var problem = await ProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("Account already exists", problem.GetProperty("title").GetString());
        Assert.Equal("The signed-in user already has an account.", problem.GetProperty("detail").GetString());
    }

    private async Task<string> RegisterAsync(string email)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", Registration(email, "Secret123", "Ada", "Lovelace", "1815-12-10"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static object Registration(string email, string password, string firstName, string lastName, string dateOfBirth) => new
    {
        email,
        password,
        firstName,
        lastName,
        dateOfBirth,
    };
}