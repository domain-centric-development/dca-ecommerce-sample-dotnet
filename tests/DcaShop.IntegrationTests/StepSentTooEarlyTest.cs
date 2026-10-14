using System.Net;
using System.Text.RegularExpressions;

using DcaShop.Checkout.Application.CheckoutCompletion.SubmitDelivery;
using DcaShop.Checkout.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A step's form sent before the earlier steps are done leads to the step that is still open, which says why, and
/// changes nothing — through the real HTTP pipeline. One cookie jar is one browser; redirects are not followed, each one is an assertion.
/// </summary>
public sealed class StepSentTooEarlyTest : IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>A delivery address and shipping option the delivery step accepts.</summary>
    private static readonly (string, string)[] DeliveryFields =
    [
        ("street", "Hauptstrasse 1"), ("streetLine2", string.Empty), ("city", "Berlin"), ("state", string.Empty),
        ("postalCode", "10115"), ("country", "Germany"), ("shippingOptionId", "standard"),
    ];

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
        Assert.Equal("/checkout/payment", landed);
        var payment = await guest.GetStringAsync(landed);
        Assert.Equal("Please complete the previous checkout steps first.", AvailabilityPages.Text(payment, @"data-test=""payment-error-message"">([^<]*)</div>"));
        var confirmation = await guest.GetAsync("/checkout/confirmation");
        Assert.Equal("/cart", confirmation.Headers.Location!.ToString());
        var cart = await guest.GetStringAsync(confirmation.Headers.Location);
        Assert.Equal("No confirmed order found", AvailabilityPages.Text(cart, @"data-test=""cart-error-message"">([^<]*)</div>"));
    }

    [Fact]
    public async Task AStepRefusedAsSentTooEarlyThatStepAccessGrantsSaysWhyItself()
    {
        // The page's translation alone: the delivery's submit refuses as sent before the earlier steps, while step
        // access grants the delivery step, so no step is still open to send the customer to.
        var shop = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<ISubmitDeliveryInputPort>(new RefusingAsSentTooEarly())));
        var guest = shop.CreateDefaultClient(new CookieContainerHandler(new CookieContainer()));
        await GuestOnTheDeliveryStepAsync(guest);

        var sent = await guest.PostAsync(
            "/checkout/delivery", Form(await guest.GetStringAsync("/checkout/delivery"), DeliveryFields));

        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        Assert.Equal(
            "Please complete the previous checkout steps first.",
            AvailabilityPages.Text(await sent.Content.ReadAsStringAsync(), @"data-test=""delivery-error-message"">([^<]*)</div>"));
    }

    /// <summary>Puts "Domain-Driven Design" into the cart, starts the checkout and submits the buyer and delivery steps.</summary>
    private static async Task GuestOnThePaymentStepAsync(HttpClient client)
    {
        await GuestOnTheDeliveryStepAsync(client);
        await Step(client, "/checkout/delivery", "/checkout/payment", DeliveryFields);
    }

    /// <summary>Puts "Domain-Driven Design" into the cart, starts the checkout and submits the buyer step.</summary>
    private static async Task GuestOnTheDeliveryStepAsync(HttpClient client)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(client, "Domain-Driven Design");
        var productPage = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", "1")));
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal("/checkout/buyer", started.Headers.Location!.ToString());
        await Step(client, "/checkout/buyer", "/checkout/delivery",
            ("email", "jane@example.com"), ("firstName", "Jane"), ("lastName", "Doe"), ("phone", "+49 30 1234567"));
    }

    /// <summary>Submits the step at <paramref name="page"/> with the form that page renders.</summary>
    private static async Task Step(HttpClient client, string page, string expectedNext, params (string, string)[] fields)
    {
        var response = await client.PostAsync(page, Form(await client.GetStringAsync(page), fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedNext, response.Headers.Location!.ToString());
    }

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }

    /// <summary>A delivery submit that refuses every command as sent before the buyer step was done.</summary>
    private sealed class RefusingAsSentTooEarly : ISubmitDeliveryInputPort
    {
        public Task<SubmitDeliveryResult> ExecuteAsync(SubmitDeliveryCommand command, CancellationToken cancellationToken = default) =>
            throw new CheckoutStepNotCompletedException(CheckoutSessionId.Generate(), CheckoutStep.BuyerInfo);
    }
}