using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>The delivery step asks a guest where the order goes and how fast, and refuses what it cannot take, through the real HTTP pipeline.</summary>
public sealed class DeliveryStepTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Refusal = "Please check the delivery address you entered.";

    private static readonly Regex Field = new(@"<label class=""form__label"" for=""\w+"">([^<]*)</label>\s*<input ([^>]*)>", RegexOptions.Compiled);

    private readonly WebApplicationFactory<Program> _factory;

    public DeliveryStepTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheDeliveryStepAsksForTheShippingAddressWithFourRequiredFields()
    {
        var client = await GuestWhoSubmittedTheBuyerStepAsync();

        var deliveryStep = await client.GetStringAsync("/checkout/delivery");

        var form = AvailabilityPages.Section(deliveryStep, "delivery-form");
        Assert.Equal("Shipping Address", SectionTitles(form)[0]);
        Assert.Equal(
            new FormField[]
            {
                new("Street Address", Required: true),
                new("Address Line 2 (Optional)", Required: false),
                new("City", Required: true),
                new("State/Province (Optional)", Required: false),
                new("Postal Code", Required: true),
                new("Country", Required: true),
            },
            Fields(form));
        Assert.Equal("Back", AvailabilityPages.Text(form, @"data-test=""delivery-back-link"">([^<]*)</a>"));
        Assert.Equal("Continue to Payment", AvailabilityPages.Text(form, @"data-test=""delivery-continue-button"">([^<]*)</button>"));
    }

    [Fact]
    public async Task TheDeliveryStepOffersFourShippingMethodsOneOfWhichMustBeChosen()
    {
        var client = await GuestWhoSubmittedTheBuyerStepAsync();

        var deliveryStep = await client.GetStringAsync("/checkout/delivery");

        var form = AvailabilityPages.Section(deliveryStep, "delivery-form");
        Assert.Equal("Shipping Method", SectionTitles(form)[1]);
        Assert.Equal(
            new ShippingChoice[]
            {
                new("Standard Shipping", "5-7 business days", "4.99 EUR", Required: true),
                new("Express Shipping", "2-3 business days", "9.99 EUR", Required: true),
                new("Overnight Shipping", "Next business day", "19.99 EUR", Required: true),
                new("Free Shipping", "7-10 business days", "0.00 EUR", Required: true),
            },
            ShippingChoices(form));
    }

    [Fact]
    public async Task TheDeliveryStepSummarisesTheOrderTheCheckoutWasStartedWith()
    {
        var client = await GuestWhoSubmittedTheBuyerStepAsync();

        var deliveryStep = await client.GetStringAsync("/checkout/delivery");

        Assert.Equal("Order Summary", AvailabilityPages.Text(deliveryStep, @"<h3 class=""order-summary__title"">([^<]*)</h3>"));
        var summary = AvailabilityPages.Section(deliveryStep, "order-summary-item");
        Assert.Equal(new[] { "Domain-Driven Design x1 54.99 EUR" }, SummaryLines(summary));
        Assert.Equal("54.99 EUR", AvailabilityPages.Text(summary, @"class=""order-summary__total""><span>Subtotal</span><span>([^<]*)</span>"));
    }

    [Fact]
    public async Task AStreetAddressOfSpacesOnlyOpensTheDeliveryStepAgainWithTheRefusal()
    {
        var client = await GuestWhoSubmittedTheBuyerStepAsync();

        var response = await SubmitAsync(client, "   ", "Berlin", "10115", "Germany");

        await AssertDeliveryStepOpensAgainWithTheRefusalAsync(response);
    }

    [Fact]
    public async Task ACityOfSpacesOnlyOpensTheDeliveryStepAgainWithTheRefusal()
    {
        var client = await GuestWhoSubmittedTheBuyerStepAsync();

        var response = await SubmitAsync(client, "Hauptstrasse 1", "   ", "10115", "Germany");

        await AssertDeliveryStepOpensAgainWithTheRefusalAsync(response);
    }

    [Fact]
    public async Task APostalCodeOfSpacesOnlyOpensTheDeliveryStepAgainWithTheRefusal()
    {
        var client = await GuestWhoSubmittedTheBuyerStepAsync();

        var response = await SubmitAsync(client, "Hauptstrasse 1", "Berlin", "   ", "Germany");

        await AssertDeliveryStepOpensAgainWithTheRefusalAsync(response);
    }

    [Fact]
    public async Task ACountryOfSpacesOnlyOpensTheDeliveryStepAgainWithTheRefusal()
    {
        var client = await GuestWhoSubmittedTheBuyerStepAsync();

        var response = await SubmitAsync(client, "Hauptstrasse 1", "Berlin", "10115", "   ");

        await AssertDeliveryStepOpensAgainWithTheRefusalAsync(response);
    }

    private static async Task AssertDeliveryStepOpensAgainWithTheRefusalAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Equal("Shipping Address", SectionTitles(AvailabilityPages.Section(page, "delivery-form"))[0]);
        Assert.Equal(Refusal, AvailabilityPages.Text(page, @"data-test=""delivery-error-message"">([^<]*)</div>"));
    }

    private async Task<HttpClient> GuestWhoSubmittedTheBuyerStepAsync()
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
        var submitted = await client.PostAsync("/checkout/buyer",
            Form(buyerStep, ("email", "jane@example.com"), ("firstName", "Jane"), ("lastName", "Doe"), ("phone", "+49 30 1234567")));
        Assert.Equal("/checkout/delivery", submitted.Headers.Location!.ToString());
        return client;
    }

    private static async Task<HttpResponseMessage> SubmitAsync(HttpClient client, string street, string city, string postalCode, string country)
    {
        var deliveryStep = await client.GetStringAsync("/checkout/delivery");
        return await client.PostAsync("/checkout/delivery",
            Form(deliveryStep,
                ("street", street), ("streetLine2", string.Empty), ("city", city), ("state", string.Empty),
                ("postalCode", postalCode), ("country", country), ("shippingOptionId", "standard")));
    }

    private static IReadOnlyList<string> SectionTitles(string form) =>
        Regex.Matches(form, @"<h3 class=""checkout__section-title"">([^<]*)</h3>").Select(match => match.Groups[1].Value.Trim()).ToList();

    private static IReadOnlyList<FormField> Fields(string form) =>
        Field.Matches(form)
            .Select(match => new FormField(match.Groups[1].Value, Regex.IsMatch(match.Groups[2].Value, @"\brequired\b")))
            .ToList();

    private static IReadOnlyList<ShippingChoice> ShippingChoices(string form) =>
        form.Split("data-test=\"delivery-shipping-option\"").Skip(1)
            .Select(option => new ShippingChoice(
                AvailabilityPages.Text(option, @"class=""option-card__label"">([^<]*)</span>"),
                AvailabilityPages.Text(option, @"class=""option-card__description"">([^<]*)</span>"),
                AvailabilityPages.Text(option, @"class=""option-card__price"">([^<]*)</span>"),
                Regex.IsMatch(AvailabilityPages.Text(option, @"(<input class=""option-card__radio""[^>]*>)"), @"\brequired\b")))
            .ToList();

    private static IReadOnlyList<string> SummaryLines(string summary) =>
        summary.Split("data-test=\"order-summary-item\"").Skip(1)
            .Select(entry => string.Join(' ',
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-name"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-qty"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"class=""order-summary__item-price"">([^<]*)</span>")))
            .ToList();

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }

    private sealed record FormField(string Label, bool Required);

    private sealed record ShippingChoice(string Name, string Estimate, string Price, bool Required);
}