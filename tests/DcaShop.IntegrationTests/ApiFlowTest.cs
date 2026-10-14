

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

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
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal("Bearer", anonymous.Headers.WwwAuthenticate.ToString());

        var (customer, _) = await _factory.RegisterThroughTheApiAsync("catalog-customer@example.com");
        var asCustomer = await _factory.Bearer(customer).PostAsync("/api/products", NewProduct());
        Assert.Equal(HttpStatusCode.Forbidden, asCustomer.StatusCode);

        var (_, staffUserId) = await _factory.RegisterThroughTheApiAsync("catalog-staff@example.com");
        var created = await _factory.Bearer(_factory.StaffToken(staffUserId, "catalog-staff@example.com")).PostAsync("/api/products", NewProduct());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task AProductAddressThatNamesNoProductAnswers404WithAnEmptyBody()
    {
        var response = await _factory.CreateClient().GetAsync("/api/products/no-such-product");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ACartRouteOnlyEverAnswersWithTheCallersOwnCart()
    {
        var (mine, _) = await _factory.RegisterThroughTheApiAsync("cart-owner@example.com");
        var (theirs, _) = await _factory.RegisterThroughTheApiAsync("cart-stranger@example.com");

        var created = await _factory.Bearer(mine).PostAsync("/api/carts", Empty());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var cartId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("cartId").GetString()!;
        var productId = await _factory.ProductIdAsync("Domain-Driven Design");
        var filled = await _factory.Bearer(mine).PostAsJsonAsync(
            $"/api/carts/{cartId}/items", new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.OK, filled.StatusCode);

        var own = await _factory.Bearer(mine).GetAsync($"/api/carts/{cartId}");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);

        // Not 403: telling a stranger they are forbidden confirms the id exists. The stranger names a cart and a
        // line that both exist, so the 404 can only come from whose cart it is.
        Assert.Equal(HttpStatusCode.NotFound, (await _factory.Bearer(theirs).GetAsync($"/api/carts/{cartId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _factory.Bearer(theirs).DeleteAsync($"/api/carts/{cartId}/items/{productId}")).StatusCode);

        var afterwards = await _factory.Bearer(mine).GetFromJsonAsync<JsonElement>($"/api/carts/{cartId}");
        var line = Assert.Single(afterwards.GetProperty("items").EnumerateArray());
        Assert.Equal(productId, line.GetProperty("productId").GetString());
        Assert.Equal(2, line.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task ListingEveryCartNeedsStaff()
    {
        var (customer, _) = await _factory.RegisterThroughTheApiAsync("cart-lister@example.com");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/carts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _factory.Bearer(customer).GetAsync("/api/carts")).StatusCode);

        var (_, staffUserId) = await _factory.RegisterThroughTheApiAsync("cart-operator@example.com");
        var listed = await _factory.Bearer(_factory.StaffToken(staffUserId, "cart-operator@example.com")).GetAsync("/api/carts");
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

    private static StringContent NewProduct() => new(
        JsonSerializer.Serialize(new
        {
            sku = $"API-{Guid.NewGuid():N}".ToUpperInvariant()[..12],
            name = "API Product",
            description = "Created through the REST API",
            imageUrl = "",
            price = 19.99m,
            category = "Books",
            stock = 5,
        }),
        Encoding.UTF8,
        "application/json");

    private static StringContent Empty() => new(string.Empty, Encoding.UTF8, "application/json");
}