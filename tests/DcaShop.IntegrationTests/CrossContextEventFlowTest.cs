using System.Net;
using System.Text.RegularExpressions;
using DcaShop.Inventory.Api;
using DcaShop.Pricing.Api;
using DcaShop.Product.Application.CreateProduct;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The event chains between the contexts: a new product gets its price and stock, a confirmed checkout
/// reduces stock, and a cart changed during checkout re-syncs the running session.
/// </summary>
public sealed class CrossContextEventFlowTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CrossContextEventFlowTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatedProductReceivesItsPriceAndStockThroughEvents()
    {
        Guid productId;
        using (var scope = _factory.Services.CreateScope())
        {
            var created = await scope.ServiceProvider.GetRequiredService<ICreateProductInputPort>().ExecuteAsync(
                new CreateProductCommand("EVT-001", "Event Sourced Lamp", "Lights up on ProductCreatedEvent", "/images/lamp.svg", 42.50m, "EUR", "Home", 7));
            productId = created.ProductId;
        }

        var product = new ProductId(productId);
        await Eventually(async () =>
        {
            using var scope = _factory.Services.CreateScope();
            var price = await scope.ServiceProvider.GetRequiredService<PricingService>().GetPriceAsync(product);
            var stock = await scope.ServiceProvider.GetRequiredService<InventoryService>().GetStockAsync(product);
            return price?.CurrentPrice == Money.Euro(42.50m) && stock?.AvailableStock == 7;
        });
    }

    [Fact]
    public async Task ConfirmedCheckoutReducesStock()
    {
        var client = Client();
        var catalog = await client.GetStringAsync("/products");
        var productId = FirstProductId(catalog);
        var stockBefore = await StockOf(productId);

        await BuyAsync(client, productId, quantity: 2);

        await Eventually(async () => await StockOf(productId) == stockBefore - 2);
    }

    [Fact]
    public async Task CartChangedDuringCheckoutSyncsTheSession()
    {
        var client = Client();
        var catalog = await client.GetStringAsync("/products");
        var productIds = Regex.Matches(catalog, @"href=""/products/([0-9a-f-]{36})""").Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.True(productIds.Count >= 2, "the catalog must offer at least two products");

        await AddToCartAsync(client, productIds[0], quantity: 1);
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);
        Assert.Single(OrderSummaryItems(await client.GetStringAsync("/checkout/buyer")));

        // The cart stays modifiable during checkout — the session must follow
        await AddToCartAsync(client, productIds[1], quantity: 3);

        await Eventually(async () => OrderSummaryItems(await client.GetStringAsync("/checkout/buyer")).Count == 2);
    }

    private HttpClient Client() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private async Task<int> StockOf(string productId)
    {
        using var scope = _factory.Services.CreateScope();
        var stock = await scope.ServiceProvider.GetRequiredService<InventoryService>().GetStockAsync(new ProductId(Guid.Parse(productId)));
        return stock?.AvailableStock ?? 0;
    }

    private static string FirstProductId(string catalog)
    {
        var productId = Regex.Match(catalog, @"href=""/products/([0-9a-f-]{36})""").Groups[1].Value;
        Assert.NotEmpty(productId);
        return productId;
    }

    private static IReadOnlyList<string> OrderSummaryItems(string page) =>
        Regex.Matches(page, "order-summary__item-name\">([^<]+)<").Select(m => m.Groups[1].Value).ToList();

    private static async Task AddToCartAsync(HttpClient client, string productId, int quantity)
    {
        var detail = await client.GetStringAsync($"/products/{productId}");
        var added = await client.PostAsync("/cart/add-product", Form(detail, ("productId", productId), ("quantity", quantity.ToString())));
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
    }

    private static async Task BuyAsync(HttpClient client, string productId, int quantity)
    {
        await AddToCartAsync(client, productId, quantity);
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);

        await Step(client, "/checkout/buyer", "/checkout/delivery",
            ("email", "ada@example.com"), ("firstName", "Ada"), ("lastName", "Lovelace"), ("phone", "0123"));
        await Step(client, "/checkout/delivery", "/checkout/payment",
            ("street", "Analytical Engine Way 1"), ("city", "London"), ("postalCode", "12345"), ("country", "UK"), ("shippingOptionId", "standard"));
        await Step(client, "/checkout/payment", "/checkout/review", ("providerId", "invoice"));
        var review = await client.GetStringAsync("/checkout/review");
        var confirmed = await client.PostAsync("/checkout/confirm", Form(review));
        Assert.Equal(HttpStatusCode.Redirect, confirmed.StatusCode);
        Assert.Equal("/checkout/confirmation", confirmed.Headers.Location!.ToString());
    }

    private static async Task Step(HttpClient client, string url, string expectedNext, params (string, string)[] fields)
    {
        var response = await client.PostAsync(url, Form(await client.GetStringAsync(url), fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedNext, response.Headers.Location!.ToString());
    }

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }

    private static async Task Eventually(Func<Task<bool>> condition)
    {
        for (var i = 0; i < 50; i++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Fail("condition not met within 5 seconds");
    }
}
