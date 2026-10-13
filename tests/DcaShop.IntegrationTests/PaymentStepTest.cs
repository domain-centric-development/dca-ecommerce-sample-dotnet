using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>The payment step shows a guest what the order costs with shipping and how it can be paid, through the real HTTP pipeline.</summary>
public sealed class PaymentStepTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PaymentStepTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ThePaymentStepOffersTheOneAvailableProvider()
    {
        var client = await GuestWhoSubmittedTheDeliveryStepAsync("standard");

        var paymentStep = await client.GetStringAsync("/checkout/payment");

        var form = AvailabilityPages.Section(paymentStep, "payment-form");
        Assert.Equal(new[] { "Select Payment Method" }, SectionTitles(form));
        Assert.Equal(new[] { "Mock Payment (Test)" }, ProviderChoices(form));
        Assert.Equal("Back", AvailabilityPages.Text(form, @"data-test=""payment-back-link"">([^<]*)</a>"));
        Assert.Equal("Continue to Review", AvailabilityPages.Text(form, @"data-test=""payment-continue-button"">([^<]*)</button>"));
    }

    [Fact]
    public async Task ThePaymentStepAddsStandardShippingToTheOrderTotal()
    {
        var client = await GuestWhoSubmittedTheDeliveryStepAsync("standard");

        var paymentStep = await client.GetStringAsync("/checkout/payment");

        Assert.Equal("Order Summary", AvailabilityPages.Text(paymentStep, @"<h3 class=""order-summary__title"">([^<]*)</h3>"));
        var summary = AvailabilityPages.Section(paymentStep, "order-summary-item");
        Assert.Equal(new[] { "Domain-Driven Design x1 54.99 EUR" }, SummaryLines(summary));
        Assert.Equal("54.99 EUR", SummaryLine(summary, "Subtotal"));
        Assert.Equal("4.99 EUR", SummaryLine(summary, "Shipping"));
        Assert.Equal("59.98 EUR", Total(summary));
    }

    [Fact]
    public async Task ThePaymentStepAddsExpressShippingToTheOrderTotal()
    {
        var client = await GuestWhoSubmittedTheDeliveryStepAsync("express");

        var paymentStep = await client.GetStringAsync("/checkout/payment");

        var summary = AvailabilityPages.Section(paymentStep, "order-summary-item");
        Assert.Equal("54.99 EUR", SummaryLine(summary, "Subtotal"));
        Assert.Equal("9.99 EUR", SummaryLine(summary, "Shipping"));
        Assert.Equal("64.98 EUR", Total(summary));
    }

    [Fact]
    public async Task AProviderTheShopDoesNotOfferOpensThePaymentStepAgainWithTheRefusal()
    {
        var client = await GuestWhoSubmittedTheDeliveryStepAsync("standard");
        var paymentStep = await client.GetStringAsync("/checkout/payment");

        var response = await client.PostAsync("/checkout/payment", Form(paymentStep, ("providerId", "paypal")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Equal(new[] { "Select Payment Method" }, SectionTitles(AvailabilityPages.Section(page, "payment-form")));
        Assert.Equal(
            "This way to pay is not offered. Please choose another one.",
            AvailabilityPages.Text(page, @"data-test=""payment-error-message"">([^<]*)</div>"));
    }

    private async Task<HttpClient> GuestWhoSubmittedTheDeliveryStepAsync(string shippingOptionId)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var productPath = await AvailabilityPages.ProductPathAsync(client, "Domain-Driven Design");
        var productPage = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", "1")));
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal("/checkout/buyer", started.Headers.Location!.ToString());
        var buyerStep = await client.GetStringAsync("/checkout/buyer");
        var buyer = await client.PostAsync("/checkout/buyer",
            Form(buyerStep, ("email", "jane@example.com"), ("firstName", "Jane"), ("lastName", "Doe"), ("phone", "+49 30 1234567")));
        Assert.Equal("/checkout/delivery", buyer.Headers.Location!.ToString());
        var deliveryStep = await client.GetStringAsync("/checkout/delivery");
        var delivery = await client.PostAsync("/checkout/delivery",
            Form(deliveryStep,
                ("street", "Hauptstrasse 1"), ("streetLine2", string.Empty), ("city", "Berlin"), ("state", string.Empty),
                ("postalCode", "10115"), ("country", "Germany"), ("shippingOptionId", shippingOptionId)));
        Assert.Equal("/checkout/payment", delivery.Headers.Location!.ToString());
        return client;
    }

    private static IReadOnlyList<string> SectionTitles(string form) =>
        Regex.Matches(form[..form.IndexOf("</form>", StringComparison.Ordinal)], @"<h3 class=""checkout__section-title"">([^<]*)</h3>")
            .Select(match => match.Groups[1].Value.Trim())
            .ToList();

    private static IReadOnlyList<string> ProviderChoices(string form) =>
        form.Split("data-test=\"payment-provider-option\"").Skip(1)
            .Select(option => AvailabilityPages.Text(option, @"class=""option-card__label"">([^<]*)</span>"))
            .ToList();

    private static IReadOnlyList<string> SummaryLines(string summary) =>
        summary.Split("data-test=\"order-summary-item\"").Skip(1)
            .Select(entry => string.Join(' ',
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-name"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-qty"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"class=""order-summary__item-price"">([^<]*)</span>")))
            .ToList();

    private static string SummaryLine(string summary, string label) =>
        AvailabilityPages.Text(summary, $@"class=""order-summary__line""><span>{Regex.Escape(label)}</span><span>([^<]*)</span>");

    private static string Total(string summary) =>
        AvailabilityPages.Text(summary, @"class=""order-summary__total""><span>Total</span><span data-test=""order-summary-total"">([^<]*)</span>");

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}