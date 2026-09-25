using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using DcaShop.Account.Adapter.Outgoing.Security;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// How a refused request reads over the API.
/// </summary>
/// <remarks>
/// Every refusal is a problem document (RFC 9457): the status carries the kind of failure, the title names it,
/// the detail is the wording of the layer that refused. What is being checked here is that the status follows
/// the failure and not the base type — a stock keeping unit the catalog already holds is a conflict, a
/// malformed one is a bad request, and both come out of the same endpoint that used to answer 400 with the
/// bare message for either (ADR-016).
/// </remarks>
public sealed class ApiProblemDetailTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiProblemDetailTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ADuplicateStockKeepingUnitIsAConflict()
    {
        var staff = await StaffClientAsync("api-problem-staff@example.com");
        var sku = $"API-DUP-{Guid.NewGuid():N}".ToUpperInvariant()[..12];

        Assert.Equal(HttpStatusCode.Created, (await staff.PostAsync("/api/products", ProductWith(sku))).StatusCode);

        var conflict = await staff.PostAsync("/api/products", ProductWith(sku));

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);
        var problem = await conflict.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, problem.GetProperty("status").GetInt32());
        Assert.Equal("Stock keeping unit already in use", problem.GetProperty("title").GetString());
        Assert.Contains(sku, problem.GetProperty("detail").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AValueTheModelRefusesIsABadRequest()
    {
        var staff = await StaffClientAsync("api-problem-staff-2@example.com");

        var rejected = await staff.PostAsync("/api/products", ProductWith("lower case sku"));

        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Unacceptable value", (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
    }

    [Fact]
    public async Task AnAddressAnotherAccountHoldsIsAConflict()
    {
        await RegisterAsync("api-problem-taken@example.com");

        var again = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", Registration("api-problem-taken@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("application/problem+json", again.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Email already registered", (await again.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
    }

    [Fact]
    public async Task AnArticleTheAssortmentDoesNotCarryIsNotFound()
    {
        var (token, _) = await RegisterAsync("api-problem-shopper@example.com");
        var client = Bearer(token);

        var created = await client.PostAsync("/api/carts", Empty());
        var cartId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("cartId").GetString()!;

        var refused = await client.PostAsJsonAsync($"/api/carts/{cartId}/items", new { productId = Guid.NewGuid(), quantity = 1 });

        Assert.Equal(HttpStatusCode.NotFound, refused.StatusCode);
        Assert.Equal("application/problem+json", refused.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Article not available", (await refused.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
    }

    private HttpClient Bearer(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<HttpClient> StaffClientAsync(string email)
    {
        var (_, userId) = await RegisterAsync(email);
        return Bearer(StaffToken(userId, email));
    }

    private async Task<(string Token, string UserId)> RegisterAsync(string email)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", Registration(email));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        return (token, SubjectOf(token));
    }

    private string StaffToken(string userId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<JwtTokenService>().GenerateRegisteredToken(
            UserId.Of(userId),
            email,
            new HashSet<string> { IIdentityProvider.IIdentity.RoleCustomer, IIdentityProvider.IIdentity.RoleStaff });
    }

    private static object Registration(string email) => new
    {
        email,
        password = "Secret123",
        firstName = "Ada",
        lastName = "Lovelace",
        dateOfBirth = "1815-12-10",
    };

    private static string SubjectOf(string jwt)
    {
        var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        var claims = JsonSerializer.Deserialize<JsonElement>(Convert.FromBase64String(payload.PadRight((payload.Length + 3) / 4 * 4, '=')));
        return claims.GetProperty("sub").GetString()!;
    }

    private static StringContent ProductWith(string sku) => new(
        JsonSerializer.Serialize(new
        {
            sku,
            name = "API Product",
            description = "d",
            imageUrl = "",
            price = 19.99m,
            category = "Books",
            stock = 5,
        }),
        Encoding.UTF8,
        "application/json");

    private static StringContent Empty() => new(string.Empty, Encoding.UTF8, "application/json");
}