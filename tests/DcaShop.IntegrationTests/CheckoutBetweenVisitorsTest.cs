using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A guest's checkout stays theirs while a second visitor knows the cart id — through the real HTTP pipeline and the
/// real session store. One cookie jar is one browser; redirects are not followed, each one is an assertion.
/// </summary>
public sealed class CheckoutBetweenVisitorsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CheckoutBetweenVisitorsTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AStrangerStartingFromTheOwnersCartIsSentToTheirCartAndTheOwnersCheckoutStays()
    {
        var owner = Browser(new CookieContainer());
        var ownersCartId = await OwnerOnTheDeliveryStepAsync(owner);
        var stranger = Browser(new CookieContainer());
        // An empty cart page renders no form, so the stranger takes their antiforgery token from a page that does
        var strangersPage = await stranger.GetStringAsync(await AvailabilityPages.ProductPathAsync(stranger, "Domain-Driven Design"));

        var started = await stranger.PostAsync("/checkout/start", Form(strangersPage, ("cartId", ownersCartId)));

        Assert.Equal("/cart?notice=cart-not-found", started.Headers.Location!.ToString());
        Assert.Equal("Your cart could not be found.", await CartErrorAsync(stranger, started.Headers.Location!));
        var ownersBuyerStep = await owner.GetStringAsync("/checkout/buyer");
        Assert.Equal(new[] { "Domain-Driven Design x1" }, SummaryItems(ownersBuyerStep));
        Assert.Equal("jane@example.com", AvailabilityPages.Text(ownersBuyerStep, @"value=""([^""]*)"" data-test=""buyer-email-input"""));
    }

    private HttpClient Browser(CookieContainer cookies) =>
        _factory.CreateDefaultClient(new CookieContainerHandler(cookies));

    /// <summary>The error the cart page shows next, where the redirect leads.</summary>
    private static async Task<string> CartErrorAsync(HttpClient client, Uri location) =>
        AvailabilityPages.Text(await client.GetStringAsync(location), @"data-test=""cart-error-message"">([^<]*)</div>");

    /// <summary>
    /// Puts "Domain-Driven Design" into the cart, starts the checkout and submits the buyer step with
    /// "jane@example.com"; answers the cart's id.
    /// </summary>
    private static async Task<string> OwnerOnTheDeliveryStepAsync(HttpClient client)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(client, "Domain-Driven Design");
        var productPage = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", "1")));
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal("/checkout/buyer", started.Headers.Location!.ToString());
        var submitted = await client.PostAsync("/checkout/buyer", Form(await client.GetStringAsync("/checkout/buyer"),
            ("email", "jane@example.com"), ("firstName", "Jane"), ("lastName", "Doe"), ("phone", "+49 30 1234567")));
        Assert.Equal("/checkout/delivery", submitted.Headers.Location!.ToString());
        return cartId;
    }

    /// <summary>The order summary's lines as <c>name xquantity</c>, in page order.</summary>
    private static IReadOnlyList<string> SummaryItems(string page) =>
        page.Split("data-test=\"order-summary-item\"").Skip(1)
            .Select(entry => string.Join(' ',
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-name"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-qty"">([^<]*)</span>")))
            .ToList();

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}