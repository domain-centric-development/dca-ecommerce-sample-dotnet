using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>The buyer step summarises the cart the checkout was started from, through the real HTTP pipeline.</summary>
public sealed class BuyerStepSummaryTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BuyerStepSummaryTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheBuyerStepSummarisesEachCartLineAndTheSubtotal()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        await AddOneUnitAsync(client, "Domain-Driven Design");
        await AddOneUnitAsync(client, "Domain-Driven Design");
        await AddOneUnitAsync(client, "Clean Architecture");
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;

        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));

        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);
        var buyerStep = await client.GetStringAsync(started.Headers.Location!.ToString());
        Assert.Equal("Order Summary", AvailabilityPages.Text(buyerStep, @"<h3 class=""order-summary__title"">([^<]*)</h3>"));
        var summary = AvailabilityPages.Section(buyerStep, "order-summary-item");
        Assert.Equal(
            new SummaryEntry[]
            {
                new("/images/products/ddd-book.webp", "Domain-Driven Design", "x2", "109.98 EUR"),
                new("/images/products/clean-architecture-book.webp", "Clean Architecture", "x1", "39.99 EUR"),
            },
            Entries(summary));
        Assert.Equal("149.97 EUR", AvailabilityPages.Text(summary, @"class=""order-summary__total""><span>Subtotal</span><span>([^<]*)</span>"));
    }

    private static IReadOnlyList<SummaryEntry> Entries(string summary) =>
        summary.Split("data-test=\"order-summary-item\"").Skip(1)
            .Select(entry => new SummaryEntry(
                AvailabilityPages.Text(entry, @"<img src=""([^""]*)"""),
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-name"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-qty"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"class=""order-summary__item-price"">([^<]*)</span>")))
            .ToList();

    private static async Task AddOneUnitAsync(HttpClient client, string productName)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(client, productName);
        var productPage = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", "1")));
    }

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }

    private sealed record SummaryEntry(string ImageSource, string Name, string Quantity, string LineTotal);
}