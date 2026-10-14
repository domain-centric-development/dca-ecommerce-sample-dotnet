using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A step's form sent before the earlier steps are done leads to the step that is still open, which says why, and
/// changes nothing — through the real HTTP pipeline. One cookie jar is one browser; redirects are not followed, each one is an assertion.
/// </summary>
public sealed class StepSentTooEarlyTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StepSentTooEarlyTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PlaceOrderSentFromThePaymentStepLeadsToThePaymentStepAndPlacesNoOrder()
    {
        var guest = _factory.CreateDefaultClient(new CookieContainerHandler(new CookieContainer()));
        await GuestOnThePaymentStepAsync(guest);

        var placed = await guest.PostAsync("/checkout/confirm", Form(await guest.GetStringAsync("/checkout/payment")));

        Assert.Equal(HttpStatusCode.Redirect, placed.StatusCode);
        var landed = placed.Headers.Location!.OriginalString;
        Assert.Equal("/checkout/payment?notice=earlier-steps-first", landed);
        var payment = await guest.GetStringAsync(landed);
        Assert.Equal("Please complete the previous checkout steps first.", AvailabilityPages.Text(payment, @"data-test=""payment-error-message"">([^<]*)</div>"));
        var confirmation = await guest.GetAsync("/checkout/confirmation");
        Assert.Equal("/cart", confirmation.Headers.Location!.ToString());
        var cart = await guest.GetStringAsync("/cart");
        Assert.Equal("No confirmed order found", AvailabilityPages.Text(cart, @"data-test=""cart-error-message"">([^<]*)</div>"));
    }

    /// <summary>Puts "Domain-Driven Design" into the cart, starts the checkout and submits the buyer and delivery steps.</summary>
    private static async Task GuestOnThePaymentStepAsync(HttpClient client)
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
        await Step(client, "/checkout/delivery", "/checkout/payment", "/checkout/delivery",
            ("street", "Hauptstrasse 1"), ("streetLine2", string.Empty), ("city", "Berlin"), ("state", string.Empty),
            ("postalCode", "10115"), ("country", "Germany"), ("shippingOptionId", "standard"));
    }

    private static async Task Step(HttpClient client, string page, string expectedNext, string url, params (string, string)[] fields)
    {
        var response = await client.PostAsync(url, Form(await client.GetStringAsync(page), fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedNext, response.Headers.Location!.ToString());
    }

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}