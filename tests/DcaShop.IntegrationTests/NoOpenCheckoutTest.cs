using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Without an open checkout the step pages send the visitor to the cart with a reason, and the confirmation stays
/// available after an order — through the real HTTP pipeline. One cookie jar is one browser; redirects are not
/// followed, each one is an assertion.
/// </summary>
public sealed class NoOpenCheckoutTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NoOpenCheckoutTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ALaterStepWithoutACheckoutLeadsToTheCartWithAMessage()
    {
        var visitor = Browser(_factory, new CookieContainer());

        var response = await visitor.GetAsync("/checkout/review");

        Assert.Equal("/cart", response.Headers.Location!.ToString());
        Assert.Equal("No active checkout session found", await CartErrorAsync(visitor, response.Headers.Location!));
    }

    [Fact]
    public async Task TheStepsAfterThePlacedOrderLeadToTheCartWithAMessage()
    {
        var guest = Browser(_factory, new CookieContainer());
        await GuestOnTheDeliveryStepAsync(guest);
        await PlaceTheOrderAsync(guest);
        Assert.Equal(HttpStatusCode.OK, (await guest.GetAsync("/checkout/confirmation")).StatusCode);

        var response = await guest.GetAsync("/checkout/buyer");

        Assert.Equal("/cart", response.Headers.Location!.ToString());
        Assert.Equal("No active checkout session found", await CartErrorAsync(guest, response.Headers.Location!));
    }

    [Fact]
    public async Task ARestartOfTheShopEndsAnOpenCheckout()
    {
        var cookies = new CookieContainer();
        var beforeRestart = Browser(_factory.WithWebHostBuilder(_ => { }), cookies);
        await GuestOnTheDeliveryStepAsync(beforeRestart);
        Assert.Equal(HttpStatusCode.OK, (await beforeRestart.GetAsync("/checkout/delivery")).StatusCode);
        var afterRestart = Browser(_factory.WithWebHostBuilder(_ => { }), cookies);

        var response = await afterRestart.GetAsync("/checkout/delivery");

        Assert.Equal("/cart", response.Headers.Location!.ToString());
        Assert.Equal("No active checkout session found", await CartErrorAsync(afterRestart, response.Headers.Location!));
    }

    [Fact]
    public async Task TheConfirmationCanBeReopenedAfterBrowsingTheCatalogue()
    {
        var guest = Browser(_factory, new CookieContainer());
        await GuestOnTheDeliveryStepAsync(guest);
        await PlaceTheOrderAsync(guest);
        await guest.GetStringAsync("/products");

        var confirmation = await guest.GetStringAsync("/checkout/confirmation");

        Assert.Equal("Your order has been placed successfully", AvailabilityPages.Text(confirmation, @"data-test=""confirmation-title""[^>]*>([^<]*)<"));
        var items = Between(confirmation, "<h4>Items Ordered</h4>", "class=\"order-summary__line\"");
        Assert.Equal(new[] { "Domain-Driven Design x1 54.99 EUR" }, ConfirmedItems(items));
    }

    [Fact]
    public async Task TheConfirmationWithoutAnOrderLeadsToTheCartWithAMessage()
    {
        var visitor = Browser(_factory, new CookieContainer());

        var response = await visitor.GetAsync("/checkout/confirmation");

        Assert.Equal("/cart", response.Headers.Location!.ToString());
        Assert.Equal("No confirmed order found", await CartErrorAsync(visitor, response.Headers.Location!));
    }

    private static HttpClient Browser(WebApplicationFactory<Program> shop, CookieContainer cookies) =>
        shop.CreateDefaultClient(new CookieContainerHandler(cookies));

    /// <summary>The error the cart page shows next, where the redirect leads.</summary>
    private static async Task<string> CartErrorAsync(HttpClient client, Uri location) =>
        AvailabilityPages.Text(await client.GetStringAsync(location), @"data-test=""cart-error-message"">([^<]*)</div>");

    /// <summary>Puts "Domain-Driven Design" into the cart, starts the checkout and submits the buyer's details.</summary>
    private static async Task GuestOnTheDeliveryStepAsync(HttpClient client)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(client, "Domain-Driven Design");
        var productPage = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", "1")));
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal("/checkout/buyer", started.Headers.Location!.ToString());
        await Step(client, "/checkout/buyer", "/checkout/delivery", "/checkout/buyer",
            ("email", "jane@example.com"), ("firstName", "Jane"), ("lastName", "Doe"), ("phone", "+49 30 1234567"));
    }

    private static async Task PlaceTheOrderAsync(HttpClient client)
    {
        await Step(client, "/checkout/delivery", "/checkout/payment", "/checkout/delivery",
            ("street", "Hauptstrasse 1"), ("streetLine2", string.Empty), ("city", "Berlin"), ("state", string.Empty),
            ("postalCode", "10115"), ("country", "Germany"), ("shippingOptionId", "standard"));
        await Step(client, "/checkout/payment", "/checkout/review", "/checkout/payment", ("providerId", "mock"));
        await Step(client, "/checkout/review", "/checkout/confirmation", "/checkout/confirm", ("placedFrom", "review"));
    }

    private static async Task Step(HttpClient client, string page, string expectedNext, string url, params (string, string)[] fields)
    {
        var response = await client.PostAsync(url, Form(await client.GetStringAsync(page), fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedNext, response.Headers.Location!.ToString());
    }

    /// <summary>The markup from <paramref name="from"/> up to the first <paramref name="to"/> after it.</summary>
    private static string Between(string page, string from, string to)
    {
        var section = page[page.IndexOf(from, StringComparison.Ordinal)..];
        return section[..section.IndexOf(to, StringComparison.Ordinal)];
    }

    private static IReadOnlyList<string> ConfirmedItems(string items) =>
        items.Split("class=\"order-summary__item\"").Skip(1)
            .Select(entry => string.Join(' ',
                AvailabilityPages.Text(entry, @"class=""order-summary__item-name"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"class=""order-summary__item-qty"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"class=""order-summary__item-price"">([^<]*)</span>")))
            .ToList();

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}