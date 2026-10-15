using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// A member of staff changing a product's price over the REST API, and what a visitor then sees on the product page
/// and the catalogue card. The change goes into a shop of the test's own, so the shared shop keeps the seeded prices
/// the other suites read.
/// </summary>
public sealed class ChangePriceE2eTest : BaseE2eTest, IDisposable
{
    private readonly SideShop _shop = SideShop.Start();

    public ChangePriceE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "A changed price shows on the product page")]
    public async Task AChangedPriceIsShownOnTheProductPageAndTheCard()
    {
        var staff = Staff.Client(_shop.BaseUrl, await Staff.TokenAsync(_shop.BaseUrl, "price-keeper@example.com"));
        var productId = await ProductIdAsync(staff, "Domain-Driven Design");
        var before = await ProductDetailPage.NavigateToAsync(Page, _shop.BaseUrl, $"/products/{productId}");
        Assert.Equal("54.99 EUR", await before.PriceAsync());

        var response = await staff.PutAsync($"/api/products/{productId}/price", new StringContent(
            """{"price":49.99}""", Encoding.UTF8, "application/json"));

        Assert.True(response.IsSuccessStatusCode, $"the change is accepted, answered {(int)response.StatusCode}");
        var productPage = await ProductDetailPage.NavigateToAsync(Page, _shop.BaseUrl, $"/products/{productId}");
        Assert.Equal("49.99 EUR", await productPage.PriceAsync());
        var catalogue = await ProductCatalogPage.NavigateToAsync(Page, _shop.BaseUrl);
        Assert.Equal("49.99 EUR", await catalogue.CardPriceAsync("Domain-Driven Design"));
    }

    public void Dispose() => _shop.Dispose();

    private static async Task<string> ProductIdAsync(HttpClient client, string name)
    {
        var products = await client.GetFromJsonAsync<JsonElement>("/api/products");
        Assert.Equal(JsonValueKind.Array, products.ValueKind);
        return products.EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == name)
            .GetProperty("id").GetString()!;
    }
}