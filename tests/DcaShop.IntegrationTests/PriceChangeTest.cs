using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using DcaShop.Pricing.Application.ChangePrice;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A price change in the wired shop over the REST API — the one-cent change it takes, the refusals after which the
/// price stays — and the use case's own refusal of a product without a price. Each test starts a shop of its own, so a changed price never reaches another test.
/// </summary>
public sealed class PriceChangeTest
{
    private static readonly Regex PagePrice = new(@"data-test=""product-detail-price"">([^<]*)<", RegexOptions.Compiled);

    [Fact]
    public async Task AStrangerIsChallengedAndThePriceStays()
    {
        using var shop = new WebApplicationFactory<Program>();
        var productId = await shop.ProductIdAsync("Domain-Driven Design");
        Assert.Equal("54.99 EUR", await PagePriceAsync(shop, productId));

        var response = await shop.CreateClient().PutAsync($"/api/products/{productId}/price", Json("""{"price":49.99}"""));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, challenge => challenge.Scheme == "Bearer");
        Assert.Equal("54.99 EUR", await PagePriceAsync(shop, productId));
    }

    [Fact]
    public async Task ACustomerIsForbiddenAndThePriceStays()
    {
        using var shop = new WebApplicationFactory<Program>();
        var productId = await shop.ProductIdAsync("Domain-Driven Design");
        Assert.Equal("54.99 EUR", await PagePriceAsync(shop, productId));
        var ada = await shop.CustomerClientAsync("ada@example.com");

        var response = await ada.PutAsync($"/api/products/{productId}/price", Json("""{"price":49.99}"""));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("54.99 EUR", await PagePriceAsync(shop, productId));
    }

    [Fact]
    public async Task APriceOfZeroIsAnUnacceptableValueNamingThePriceAndThePriceStays()
    {
        using var shop = new WebApplicationFactory<Program>();
        var productId = await shop.ProductIdAsync("Domain-Driven Design");
        Assert.Equal("54.99 EUR", await PagePriceAsync(shop, productId));
        var staff = await shop.StaffClientAsync("price-keeper@example.com");

        var response = await staff.PutAsync($"/api/products/{productId}/price", Json("""{"price":0}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        var reasons = problem.GetProperty("errors").GetProperty("price").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains(reasons, reason => reason!.Contains("greater than zero", StringComparison.Ordinal));
        Assert.Equal("54.99 EUR", await PagePriceAsync(shop, productId));
    }

    [Fact]
    public async Task APriceOfOneCentIsTakenAndShownOnTheProductPage()
    {
        using var shop = new WebApplicationFactory<Program>();
        var productId = await shop.ProductIdAsync("Domain-Driven Design");
        var staff = await shop.StaffClientAsync("price-keeper@example.com");

        var response = await staff.PutAsync($"/api/products/{productId}/price", Json("""{"price":0.01}"""));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("0.01 EUR", await PagePriceAsync(shop, productId));
    }

    [Fact]
    public async Task AChangeForAProductWithoutAPriceIsRefusedAsNotFound()
    {
        using var shop = new WebApplicationFactory<Program>();
        var unknown = Guid.Parse("00000000-0000-4000-8000-000000000001");
        using var scope = shop.Services.CreateScope();
        var changePrice = scope.ServiceProvider.GetRequiredService<IChangePriceInputPort>();

        var refusal = await Assert.ThrowsAsync<ProductPriceNotFoundException>(() =>
            changePrice.ExecuteAsync(new ChangePriceCommand(unknown, 49.99m, "EUR")));

        Assert.Equal(unknown, refusal.ProductId.Value);
    }

    private static async Task<string> PagePriceAsync(WebApplicationFactory<Program> shop, string productId)
    {
        var page = await shop.CreateClient().GetStringAsync($"/products/{productId}");
        var match = PagePrice.Match(page);
        Assert.True(match.Success, "the product page shows a price");
        return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");
}