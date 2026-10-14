using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using DcaShop.Account.Adapter.Incoming.Security;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The callers of the shop's REST API a test needs: a customer registered through the API, an operator holding a
/// staff token, and a client that sends either token.
/// </summary>
internal static class ApiCallers
{
    /// <summary>A client that sends <paramref name="token"/> as its Bearer token.</summary>
    internal static HttpClient Bearer(this WebApplicationFactory<Program> shop, string token)
    {
        var client = shop.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Registers a customer under <paramref name="email"/> and answers the token and the user it names.</summary>
    internal static async Task<(string Token, string UserId)> RegisterThroughTheApiAsync(
        this WebApplicationFactory<Program> shop, string email)
    {
        var response = await shop.CreateClient().PostAsJsonAsync("/api/auth/register", Registration(email));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        return (token, SubjectOf(token));
    }

    /// <summary>A client of a customer registered under <paramref name="email"/>, sending the customer's token.</summary>
    internal static async Task<HttpClient> CustomerClientAsync(this WebApplicationFactory<Program> shop, string email) =>
        shop.Bearer((await shop.RegisterThroughTheApiAsync(email)).Token);

    /// <summary>The registration form of a customer with a usable password, a name and a date of birth.</summary>
    internal static object Registration(string email) => new
    {
        email,
        password = "Secret123",
        firstName = "Ada",
        lastName = "Lovelace",
        dateOfBirth = "1815-12-10",
    };

    /// <summary>
    /// A staff token for an existing account. No registration path hands the role out, so the test mints the
    /// token the way an operator provisioning tool would.
    /// </summary>
    internal static string StaffToken(this WebApplicationFactory<Program> shop, string userId, string email)
    {
        using var scope = shop.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<JwtTokenService>().GenerateRegisteredToken(
            UserId.Of(userId),
            email,
            new HashSet<Role> { Role.Customer, Role.Staff });
    }

    /// <summary>A client of an operator: registered under <paramref name="email"/>, sending a staff token.</summary>
    internal static async Task<HttpClient> StaffClientAsync(this WebApplicationFactory<Program> shop, string email)
    {
        var (_, userId) = await shop.RegisterThroughTheApiAsync(email);
        return shop.Bearer(shop.StaffToken(userId, email));
    }

    /// <summary>The id the catalogue API gives the product named <paramref name="name"/>.</summary>
    internal static async Task<string> ProductIdAsync(this WebApplicationFactory<Program> shop, string name)
    {
        var products = await shop.CreateClient().GetFromJsonAsync<JsonElement>("/api/products");
        return products.EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == name)
            .GetProperty("id").GetString()!;
    }

    /// <summary>The user a token names: its <c>sub</c> claim.</summary>
    internal static string SubjectOf(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return json.RootElement.GetProperty("sub").GetString()!;
    }
}