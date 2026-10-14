using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// The price a member of staff gives a new product over the REST API, as the answer carries it and as a visitor then
/// sees it on the catalogue card. The product goes into a shop of the test's own, so the shared shop keeps the seeded
/// catalogue the other suites count.
/// </summary>
public sealed class CreatedProductPriceE2eTest : BaseE2eTest, IDisposable
{
    private readonly SideShop _shop = SideShop.Start();

    public CreatedProductPriceE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "A price is rounded half up to cents")]
    public async Task APriceWithAHalfCentIsRoundedUpOnTheAnswerAndTheCard()
    {
        var staff = Bearer(await StaffTokenAsync("rounding-mug@example.com"));

        var response = await staff.PostAsync("/api/products", new StringContent(
            """{"sku":"ROUND-001","name":"Rounding Mug","description":"","imageUrl":"","price":14.995,"category":"Desk & Office","stock":5}""",
            System.Text.Encoding.UTF8,
            "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("15.00", body.GetProperty("price").GetRawText());
        Assert.Equal("15.00 EUR", await CardPriceOncePricedAsync("Rounding Mug"));
    }

    public void Dispose() => _shop.Dispose();

    /// <summary>
    /// The card's price once Pricing has taken it over from the creation: the catalogue is opened again until the card
    /// shows more than the "0.00 EUR" of a product nobody has priced yet.
    /// </summary>
    private async Task<string> CardPriceOncePricedAsync(string name)
    {
        for (var i = 0; i < 50; i++)
        {
            var price = await (await ProductCatalogPage.NavigateToAsync(Page, _shop.BaseUrl)).CardPriceAsync(name);
            if (price != "0.00 EUR")
            {
                return price;
            }

            await Task.Delay(100);
        }

        Assert.Fail($"the card of {name} shows no price within 5 seconds");
        throw new InvalidOperationException("unreachable");
    }

    private HttpClient Bearer(string token) => Staff.Client(_shop.BaseUrl, token);

    private Task<string> StaffTokenAsync(string email) => Staff.TokenAsync(_shop.BaseUrl, email);
}