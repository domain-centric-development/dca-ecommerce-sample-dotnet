using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DcaShop.E2eTests;

/// <summary>
/// Logging in over the REST API, as a partner application meets it: no browser, an account registered through the
/// API. Each test registers an address of its own, so the shared shop's other accounts stay untouched.
/// </summary>
public sealed class LoginApiE2eTest
{
    [E2eFact(DisplayName = "A wrong password is refused")]
    public async Task AWrongPasswordIsAProblemDocumentWithTheBearerChallenge()
    {
        var email = $"ada-{Guid.NewGuid():N}@example.com";
        using var client = new HttpClient { BaseAddress = new Uri(ShopUnderTest.BaseUrl) };
        var registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123",
            firstName = "Ada",
            lastName = "Lovelace",
            dateOfBirth = "1815-12-10",
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong1234" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Authentication failed", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
    }
}