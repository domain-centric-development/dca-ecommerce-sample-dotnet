using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using DcaShop.Account.Adapter.Incoming.Security;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DcaShop.E2eTests;

/// <summary>
/// A member of staff as an operator's tool meets the shop: a Bearer token carrying the staff role, and a client that
/// sends it to the REST API.
/// </summary>
public static class Staff
{
    /// <summary>
    /// A staff token for a freshly registered account of the shop at <paramref name="shopUrl"/>. No registration path
    /// hands the role out, so the token is minted the way an operator provisioning tool would, with the secret the
    /// shop is started with.
    /// </summary>
    public static async Task<string> TokenAsync(string shopUrl, string email)
    {
        using var client = new HttpClient { BaseAddress = new Uri(shopUrl) };
        var registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123",
            firstName = "Ada",
            lastName = "Lovelace",
            dateOfBirth = "1815-12-10",
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var token = (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;

        var tokens = new JwtTokenService(
            Options.Create(new JwtOptions { Secret = JwtOptions.DevelopmentSecret }),
            NullLogger<JwtTokenService>.Instance);
        return tokens.GenerateRegisteredToken(
            UserId.Of(SubjectOf(token)),
            email,
            new HashSet<Role> { Role.Customer, Role.Staff });
    }

    /// <summary>A client of the shop at <paramref name="shopUrl"/> that sends <paramref name="token"/> as Bearer token.</summary>
    public static HttpClient Client(string shopUrl, string token)
    {
        var client = new HttpClient { BaseAddress = new Uri(shopUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string SubjectOf(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return json.RootElement.GetProperty("sub").GetString()!;
    }
}