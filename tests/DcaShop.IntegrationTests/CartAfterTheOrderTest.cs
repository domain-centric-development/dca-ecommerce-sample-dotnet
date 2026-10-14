using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>What the cart page shows once an order has been placed, through the real HTTP pipeline.</summary>
public sealed class CartAfterTheOrderTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Pin = "\"Bounded Context\" Enamel Pin";
    private const string Hoodie = "\"Domain over Framework\" Hoodie";
    private const string TShirt = "\"Ports & Adapters\" T-Shirt";

    private readonly WebApplicationFactory<Program> _factory;

    public CartAfterTheOrderTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ALineBoughtWholeLeavesTheCartEmpty()
    {
        var client = Client();
        await AddUnitsAsync(client, Pin, 3);
        await StartCheckoutAsync(client);

        await PlaceOrderAsync(client);

        var cartPage = await CartPageOnceAsync(client, page => !page.Contains("data-test=\"cart-item\"", StringComparison.Ordinal));
        Assert.Equal("Your cart is empty", AvailabilityPages.Text(cartPage, @"<h3 class=""empty-state__title"">([^<]*)</h3>"));
    }

    [Fact]
    public async Task UnitsAddedAfterTheStartStayInTheCart()
    {
        var client = Client();
        await AddUnitsAsync(client, Pin, 3);
        await StartCheckoutAsync(client);
        await AddUnitsAsync(client, Pin, 2);
        Assert.Equal(new[] { Pin + " x5" }, CartLines(await client.GetStringAsync("/cart")));

        await PlaceOrderAsync(client);

        var cartPage = await CartPageOnceAsync(client, page => !CartLines(page).Contains(Pin + " x5"));
        Assert.Equal(new[] { Pin + " x2" }, CartLines(cartPage));
    }

    [Fact]
    public async Task TheCartTakesNewItemsAfterTheOrder()
    {
        var client = Client();
        await AddUnitsAsync(client, Pin, 1);
        await StartCheckoutAsync(client);
        await AddUnitsAsync(client, TShirt, 1);
        await PlaceOrderAsync(client);
        await CartPageOnceAsync(client, page => CartLines(page).SequenceEqual(new[] { TShirt + " x1" }));

        await AddUnitsAsync(client, Hoodie, 1);

        var cartPage = await client.GetStringAsync("/cart");
        Assert.Equal(new[] { TShirt + " x1", Hoodie + " x1" }, CartLines(cartPage));
        Assert.Equal("Proceed to Checkout", AvailabilityPages.Text(cartPage, @"data-test=""cart-checkout-button"">([^<]*)</button>"));
    }

    private HttpClient Client() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task AddUnitsAsync(HttpClient client, string productName, int quantity)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(client, productName);
        var productPage = await client.GetStringAsync(productPath);
        var added = await client.PostAsync("/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", quantity.ToString())));
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
    }

    /// <summary>Presses "Proceed to Checkout" on the cart page.</summary>
    private static async Task StartCheckoutAsync(HttpClient client)
    {
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal("/checkout/buyer", started.Headers.Location!.ToString());
    }

    /// <summary>Answers the buyer, delivery and payment steps of the open checkout and places the order from the review.</summary>
    private static async Task PlaceOrderAsync(HttpClient client)
    {
        await StepAsync(client, "/checkout/buyer", "/checkout/delivery",
            ("email", "guest@example.com"), ("firstName", "Test"), ("lastName", "Guest"), ("phone", "+1-555-0100"));
        await StepAsync(client, "/checkout/delivery", "/checkout/payment",
            ("street", "123 Main Street"), ("city", "Springfield"), ("postalCode", "12345"), ("country", "United States"), ("shippingOptionId", "express"));
        await StepAsync(client, "/checkout/payment", "/checkout/review", ("providerId", "mock"));
        var review = await client.GetStringAsync("/checkout/review");
        var placed = await client.PostAsync("/checkout/confirm", Form(review, ("placedFrom", "review")));
        Assert.Equal("/checkout/confirmation", placed.Headers.Location!.ToString());
    }

    private static async Task StepAsync(HttpClient client, string url, string expectedNext, params (string, string)[] fields)
    {
        var response = await client.PostAsync(url, Form(await client.GetStringAsync(url), fields));
        Assert.Equal(expectedNext, response.Headers.Location!.ToString());
    }

    /// <summary>The cart page once <paramref name="settled"/> holds for it, or after 15 seconds as it is then.</summary>
    private static async Task<string> CartPageOnceAsync(HttpClient client, Func<string, bool> settled)
    {
        var page = await client.GetStringAsync("/cart");
        for (var attempt = 0; attempt < 150 && !settled(page); attempt++)
        {
            await Task.Delay(100);
            page = await client.GetStringAsync("/cart");
        }

        return page;
    }

    /// <summary>The cart page's lines as <c>name xquantity</c>, in display order.</summary>
    private static IReadOnlyList<string> CartLines(string cartPage) =>
        cartPage.Split("data-test=\"cart-item\"").Skip(1)
            .Select(line =>
                AvailabilityPages.Text(line, @"data-test=""cart-item-name"">([^<]*)</div>") + " x" +
                AvailabilityPages.Text(line, @"data-test=""cart-item-quantity"">([^<]*)</span>"))
            .ToList();

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}