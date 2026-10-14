using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The review step shows a guest everything entered and what the order costs; placing the order opens the
/// confirmation, which repeats what was ordered — through the real HTTP pipeline.
/// </summary>
public sealed class ReviewAndConfirmationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReviewAndConfirmationTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheReviewShowsTheBuyerTheDeliveryAndTheChosenProvider()
    {
        var client = await GuestWhoChoseThePaymentProviderAsync();

        var review = await client.GetStringAsync("/checkout/review");

        var buyer = Between(review, "data-test=\"review-buyer-section\"", "data-test=\"review-delivery-section\"");
        Assert.Equal("Buyer Information", SectionTitle(buyer));
        Assert.Equal(new[] { "Jane Doe", "jane@example.com", "+49 30 1234567" }, Paragraphs(buyer));
        var delivery = Between(review, "data-test=\"review-delivery-section\"", "data-test=\"review-payment-section\"");
        Assert.Equal("Delivery", SectionTitle(delivery));
        Assert.Equal(new[] { "Hauptstrasse 1", "Berlin, 10115", "Germany" }, Paragraphs(Between(delivery, "<h4>Shipping Address</h4>", "<h4>Shipping Method</h4>")));
        Assert.Equal(new[] { "Standard Shipping", "Estimated Delivery: 5-7 business days", "Cost: 4.99 EUR" }, Paragraphs(delivery[delivery.IndexOf("<h4>Shipping Method</h4>", StringComparison.Ordinal)..]));
        var payment = Between(review, "data-test=\"review-payment-section\"", "class=\"order-summary\"");
        Assert.Equal("Payment Method", SectionTitle(payment));
        Assert.Equal("mock", Paragraphs(payment)[0]);
    }

    [Fact]
    public async Task TheReviewShowsTheItemsAndTheTotalsWithTheContainedTax()
    {
        var client = await GuestWhoChoseThePaymentProviderAsync();

        var review = await client.GetStringAsync("/checkout/review");

        Assert.Equal("Order Items", AvailabilityPages.Text(review, @"<h3 class=""order-summary__title"">([^<]*)</h3>"));
        var summary = AvailabilityPages.Section(review, "order-summary-item");
        Assert.Equal(new[] { "Domain-Driven Design x1 54.99 EUR" }, SummaryLines(summary));
        Assert.Equal("54.99 EUR", AmountLine(summary, "Subtotal"));
        Assert.Equal("4.99 EUR", AmountLine(summary, "Shipping"));
        Assert.Equal("9.58 EUR", AmountLine(summary, "Tax"));
        Assert.Equal("59.98 EUR", AvailabilityPages.Text(summary, @"class=""order-summary__total""><span>Total</span><span data-test=""order-summary-total"">([^<]*)</span>"));
        Assert.Equal("Place Order", AvailabilityPages.Text(review, @"data-test=""review-place-order-button"">([^<]*)</button>"));
    }

    [Fact]
    public async Task TheConfirmationRepeatsTheContactTheDeliveryThePaymentTheItemsAndTheAmounts()
    {
        var client = await GuestWhoChoseThePaymentProviderAsync();

        var confirmation = await PlaceTheOrderAsync(client);

        Assert.Equal(new[] { "Jane Doe", "jane@example.com", "+49 30 1234567" }, Paragraphs(DetailSection(confirmation, "Contact Information")));
        Assert.Equal(new[] { "Hauptstrasse 1", "Berlin, 10115", "Germany" }, Paragraphs(DetailSection(confirmation, "Delivery Address")));
        Assert.Equal(new[] { "Standard Shipping", "Estimated Delivery: 5-7 business days" }, Paragraphs(DetailSection(confirmation, "Shipping Method")));
        Assert.Equal(new[] { "mock" }, Paragraphs(DetailSection(confirmation, "Payment Method")));
        var items = Between(confirmation, "<h4>Items Ordered</h4>", "class=\"order-summary__line\"");
        Assert.Equal(new[] { "Domain-Driven Design x1 54.99 EUR" }, ConfirmedItems(items));
        Assert.Equal(
            new[] { "54.99 EUR", "4.99 EUR", "9.58 EUR" },
            new[] { AmountLine(confirmation, "Subtotal"), AmountLine(confirmation, "Shipping"), AmountLine(confirmation, "Tax") });
    }

    [Fact]
    public async Task ContinueShoppingOnTheConfirmationLeadsToTheHomePage()
    {
        var client = await GuestWhoChoseThePaymentProviderAsync();
        var confirmation = await PlaceTheOrderAsync(client);
        var link = Regex.Match(confirmation, @"<a [^>]*href=""([^""]*)"" data-test=""confirmation-continue-link"">([^<]*)</a>");
        Assert.Equal("Continue Shopping", link.Groups[2].Value);

        var followed = await client.GetAsync(link.Groups[1].Value);

        Assert.Equal("/", link.Groups[1].Value);
        Assert.Equal(HttpStatusCode.OK, followed.StatusCode);
        Assert.Contains("data-test=\"hero\"", await followed.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private async Task<HttpClient> GuestWhoChoseThePaymentProviderAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var productPath = await AvailabilityPages.ProductPathAsync(client, "Domain-Driven Design");
        var productPage = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", "1")));
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal("/checkout/buyer", started.Headers.Location!.ToString());
        await Step(client, "/checkout/buyer", "/checkout/delivery",
            ("email", "jane@example.com"), ("firstName", "Jane"), ("lastName", "Doe"), ("phone", "+49 30 1234567"));
        await Step(client, "/checkout/delivery", "/checkout/payment",
            ("street", "Hauptstrasse 1"), ("streetLine2", string.Empty), ("city", "Berlin"), ("state", string.Empty),
            ("postalCode", "10115"), ("country", "Germany"), ("shippingOptionId", "standard"));
        await Step(client, "/checkout/payment", "/checkout/review", ("providerId", "mock"));
        return client;
    }

    private static async Task<string> PlaceTheOrderAsync(HttpClient client)
    {
        await Step(client, "/checkout/review", "/checkout/confirmation", "/checkout/confirm", ("placedFrom", "review"));
        return await client.GetStringAsync("/checkout/confirmation");
    }

    private static async Task Step(HttpClient client, string page, string expectedNext, params (string, string)[] fields) =>
        await Step(client, page, expectedNext, page, fields);

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

    /// <summary>A confirmation detail block: what stands below its heading, up to the next heading or the block's end.</summary>
    private static string DetailSection(string page, string heading)
    {
        var section = page[(page.IndexOf($"<h4>{heading}</h4>", StringComparison.Ordinal) + heading.Length + 9)..];
        var end = new[] { section.IndexOf("<h4>", StringComparison.Ordinal), section.IndexOf("</div>", StringComparison.Ordinal) }.Where(i => i >= 0).Min();
        return section[..end];
    }

    private static string SectionTitle(string section) =>
        AvailabilityPages.Text(section, @"<h3 class=""review-section__title"">([^<]*)</h3>");

    private static IReadOnlyList<string> Paragraphs(string markup) =>
        Regex.Matches(markup, @"<p>([^<]*)</p>").Select(match => WebUtility.HtmlDecode(match.Groups[1].Value).Trim()).ToList();

    private static IReadOnlyList<string> SummaryLines(string summary) =>
        summary.Split("data-test=\"order-summary-item\"").Skip(1)
            .Select(entry => string.Join(' ',
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-name"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-qty"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"class=""order-summary__item-price"">([^<]*)</span>")))
            .ToList();

    private static IReadOnlyList<string> ConfirmedItems(string items) =>
        items.Split("class=\"order-summary__item\"").Skip(1)
            .Select(entry => string.Join(' ',
                AvailabilityPages.Text(entry, @"class=""order-summary__item-name"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"class=""order-summary__item-qty"">([^<]*)</span>"),
                AvailabilityPages.Text(entry, @"class=""order-summary__item-price"">([^<]*)</span>")))
            .ToList();

    private static string AmountLine(string markup, string label) =>
        AvailabilityPages.Text(markup, $@"class=""order-summary__line""><span>{Regex.Escape(label)}</span><span>([^<]*)</span>");

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}