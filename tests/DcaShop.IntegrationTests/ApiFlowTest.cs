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
/// The REST surface: who may see what, and the Bearer-only boundary that lets it skip the antiforgery token
/// (ADR-007). Both halves of that boundary are asserted here — no cookie authenticates an API call, and no API
/// call hands one out — because either half alone would make the exemption unsound.
/// </summary>
public sealed class ApiFlowTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiFlowTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TheCatalogIsPublicButCreatingAProductNeedsStaff()
    {
        var client = _factory.CreateClient();

        var all = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        var products = await all.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(0, products.GetArrayLength());

        var one = await client.GetAsync($"/api/products/{products[0].GetProperty("id").GetString()}");
        Assert.Equal(HttpStatusCode.OK, one.StatusCode);

        var anonymous = await client.PostAsync("/api/products", NewProduct());
        Assert.Equal(HttpStatusCode.Forbidden, anonymous.StatusCode);

        var (customer, _) = await RegisterAsync("catalog-customer@example.com");
        var asCustomer = await Bearer(customer).PostAsync("/api/products", NewProduct());
        Assert.Equal(HttpStatusCode.Forbidden, asCustomer.StatusCode);

        var (_, staffUserId) = await RegisterAsync("catalog-staff@example.com");
        var created = await Bearer(StaffToken(staffUserId, "catalog-staff@example.com")).PostAsync("/api/products", NewProduct());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task ACartRouteOnlyEverAnswersWithTheCallersOwnCart()
    {
        var (mine, _) = await RegisterAsync("cart-owner@example.com");
        var (theirs, _) = await RegisterAsync("cart-stranger@example.com");

        var created = await Bearer(mine).PostAsync("/api/carts", Empty());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var cartId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("cartId").GetString()!;

        var own = await Bearer(mine).GetAsync($"/api/carts/{cartId}");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);

        // Not 403: telling a stranger they are forbidden confirms the id exists.
        Assert.Equal(HttpStatusCode.NotFound, (await Bearer(theirs).GetAsync($"/api/carts/{cartId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Bearer(theirs).PostAsync($"/api/carts/{cartId}/checkout", Empty())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Bearer(theirs).DeleteAsync($"/api/carts/{cartId}/items/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task ListingEveryCartNeedsStaff()
    {
        var (customer, _) = await RegisterAsync("cart-lister@example.com");
        Assert.Equal(HttpStatusCode.Forbidden, (await _factory.CreateClient().GetAsync("/api/carts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Bearer(customer).GetAsync("/api/carts")).StatusCode);

        var (_, staffUserId) = await RegisterAsync("cart-operator@example.com");
        var listed = await Bearer(StaffToken(staffUserId, "cart-operator@example.com")).GetAsync("/api/carts");
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.True((await listed.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("carts", out _));
    }

    [Fact]
    public async Task ABrowserCookieNeitherAuthenticatesAnApiCallNorIsHandedOutByOne()
    {
        // A browser that has been around the shop and carries both cookies.
        var browser = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await browser.GetAsync("/");

        var response = await browser.PostAsync("/api/carts", Empty());

        // The cookies came along, and were ignored: the caller is a stranger with a cart of their own.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        var apiCustomerId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("customerId").GetString();

        var cartPage = await browser.GetStringAsync("/cart");
        Assert.DoesNotContain(apiCustomerId!, cartPage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnApiPostNeedsNoAntiforgeryTokenBecauseNoCookieCanAuthenticateIt()
    {
        var response = await _factory.CreateClient().PostAsync("/api/carts", Empty());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private HttpClient Bearer(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Registers an account through the API and returns its token and user id.</summary>
    private async Task<(string Token, string UserId)> RegisterAsync(string email)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123",
            firstName = "Ada",
            lastName = "Lovelace",
            dateOfBirth = "1815-12-10",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        return (token, SubjectOf(token));
    }

    /// <summary>
    /// A staff token for an existing account. No registration path hands the role out, so the test mints the
    /// token the way an operator provisioning tool would.
    /// </summary>
    private string StaffToken(string userId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<JwtTokenService>().GenerateRegisteredToken(
            UserId.Of(userId),
            email,
            new HashSet<string> { IIdentityProvider.IIdentity.RoleCustomer, IIdentityProvider.IIdentity.RoleStaff });
    }

    private static StringContent NewProduct() => new(
        JsonSerializer.Serialize(new
        {
            sku = $"API-{Guid.NewGuid():N}".ToUpperInvariant()[..12],
            name = "API Product",
            description = "Created through the REST API",
            imageUrl = "",
            price = 19.99m,
            category = "Electronics",
            stock = 5,
        }),
        Encoding.UTF8,
        "application/json");

    private static StringContent Empty() => new(string.Empty, Encoding.UTF8, "application/json");

    private static string SubjectOf(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return json.RootElement.GetProperty("sub").GetString()!;
    }
}
