using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A guest goes back to a step already done — through its Back and Edit links — finds it filled in, changes it and
/// keeps the progress made after it, through the real HTTP pipeline.
/// </summary>
public sealed class ChangeAnEarlierStepTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChangeAnEarlierStepTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ThePaymentStepsBackLinkOpensTheDeliveryStepWithTheAddressAndTheChosenMethod()
    {
        var client = await GuestWhoSubmittedTheDeliveryStepAsync();
        var paymentStep = await client.GetStringAsync("/checkout/payment");

        var deliveryStep = await FollowAsync(client, paymentStep, "payment-back-link");

        var form = AvailabilityPages.Section(deliveryStep, "delivery-form");
        Assert.Equal(
            new[] { "Hauptstrasse 1", "Berlin", "10115", "Germany" },
            new[] { InputValue(form, "street"), InputValue(form, "city"), InputValue(form, "postalCode"), InputValue(form, "country") });
        Assert.Equal("Standard Shipping", ChosenOption(form, "delivery-shipping-option"));
    }

    [Fact]
    public async Task TheReviewsBackLinkOpensThePaymentStepWithTheChosenProvider()
    {
        var client = await GuestOnTheReviewAsync();
        var review = await client.GetStringAsync("/checkout/review");

        var paymentStep = await FollowAsync(client, review, "review-back-link");

        Assert.Equal("Mock Payment (Test)", ChosenOption(AvailabilityPages.Section(paymentStep, "payment-form"), "payment-provider-option"));
    }

    [Fact]
    public async Task TheReviewsEditLinksOpenTheStepOfTheirSection()
    {
        var client = await GuestOnTheReviewAsync();
        var review = await client.GetStringAsync("/checkout/review");

        var deliveryStep = await FollowAsync(client, review, "review-delivery-edit-link");

        var form = AvailabilityPages.Section(deliveryStep, "delivery-form");
        Assert.Equal(
            new[] { "Hauptstrasse 1", "Berlin", "10115", "Germany" },
            new[] { InputValue(form, "street"), InputValue(form, "city"), InputValue(form, "postalCode"), InputValue(form, "country") });
        Assert.Equal("Standard Shipping", ChosenOption(form, "delivery-shipping-option"));
        Assert.Equal(("Edit", "/checkout/buyer"), Link(Between(review, "data-test=\"review-buyer-section\"", "data-test=\"review-delivery-section\""), "review-buyer-edit-link"));
        Assert.Equal(("Edit", "/checkout/payment"), Link(Between(review, "data-test=\"review-payment-section\"", "class=\"order-summary\""), "review-payment-edit-link"));
    }

    [Fact]
    public async Task ResubmittingTheDeliveryStepWithExpressShippingRecalculatesThePaymentStepsTotals()
    {
        var client = await GuestOnTheReviewAsync();
        var deliveryPath = Link(await client.GetStringAsync("/checkout/review"), "review-delivery-edit-link").Target;

        await Step(client, deliveryPath, "/checkout/payment", DeliveryFields("express"));

        var summary = AvailabilityPages.Section(await client.GetStringAsync("/checkout/payment"), "order-summary-item");
        Assert.Equal("9.99 EUR", AmountLine(summary, "Shipping"));
        Assert.Equal("64.98 EUR", Total(summary));
    }

    [Fact]
    public async Task TheReviewKeepsTheChosenProviderAndShowsTheNewShippingAfterTheDeliveryStepWasChanged()
    {
        var client = await GuestOnTheReviewAsync();
        await Step(client, "/checkout/delivery", "/checkout/payment", DeliveryFields("express"));

        var response = await client.GetAsync("/checkout/review");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var review = await response.Content.ReadAsStringAsync();
        var delivery = Between(review, "data-test=\"review-delivery-section\"", "data-test=\"review-payment-section\"");
        Assert.Equal(
            new[] { "Express Shipping", "Estimated Delivery: 2-3 business days", "Cost: 9.99 EUR" },
            Paragraphs(delivery[delivery.IndexOf("<h4>Shipping Method</h4>", StringComparison.Ordinal)..]));
        var summary = AvailabilityPages.Section(review, "order-summary-item");
        Assert.Equal(
            new[] { "54.99 EUR", "9.99 EUR", "10.37 EUR", "64.98 EUR" },
            new[] { AmountLine(summary, "Subtotal"), AmountLine(summary, "Shipping"), AmountLine(summary, "Tax"), Total(summary) });
        Assert.Equal("mock", Paragraphs(Between(review, "data-test=\"review-payment-section\"", "class=\"order-summary\""))[0]);
    }

    [Fact]
    public async Task ChangedBuyerDetailsKeepTheAddressAndReachTheReview()
    {
        var client = await GuestOnTheReviewAsync();
        var buyerPath = Link(await client.GetStringAsync("/checkout/review"), "review-buyer-edit-link").Target;

        await Step(client, buyerPath, "/checkout/delivery",
            ("email", "jane.doe@example.com"), ("firstName", "Jane"), ("lastName", "Doe"), ("phone", "+49 30 1234567"));

        var form = AvailabilityPages.Section(await client.GetStringAsync("/checkout/delivery"), "delivery-form");
        Assert.Equal(
            new[] { "Hauptstrasse 1", "Berlin", "10115", "Germany" },
            new[] { InputValue(form, "street"), InputValue(form, "city"), InputValue(form, "postalCode"), InputValue(form, "country") });
        var review = await client.GetStringAsync("/checkout/review");
        Assert.Equal("jane.doe@example.com", Paragraphs(Between(review, "data-test=\"review-buyer-section\"", "data-test=\"review-delivery-section\""))[1]);
    }

    private async Task<HttpClient> GuestWhoSubmittedTheDeliveryStepAsync()
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
        await Step(client, "/checkout/delivery", "/checkout/payment", DeliveryFields("standard"));
        return client;
    }

    private async Task<HttpClient> GuestOnTheReviewAsync()
    {
        var client = await GuestWhoSubmittedTheDeliveryStepAsync();
        await Step(client, "/checkout/payment", "/checkout/review", ("providerId", "mock"));
        return client;
    }

    private static (string, string)[] DeliveryFields(string shippingOptionId) =>
        new[]
        {
            ("street", "Hauptstrasse 1"), ("streetLine2", string.Empty), ("city", "Berlin"), ("state", string.Empty),
            ("postalCode", "10115"), ("country", "Germany"), ("shippingOptionId", shippingOptionId),
        };

    private static async Task Step(HttpClient client, string page, string expectedNext, params (string, string)[] fields)
    {
        var response = await client.PostAsync(page, Form(await client.GetStringAsync(page), fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedNext, response.Headers.Location!.ToString());
    }

    /// <summary>Follows the link carrying <paramref name="dataTest"/> and answers the page it opens.</summary>
    private static async Task<string> FollowAsync(HttpClient client, string page, string dataTest)
    {
        var response = await client.GetAsync(Link(page, dataTest).Target);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static (string Text, string Target) Link(string markup, string dataTest)
    {
        var link = Regex.Match(markup, $@"<a [^>]*href=""([^""]*)"" data-test=""{dataTest}"">([^<]*)</a>");
        Assert.True(link.Success, $"the markup has a {dataTest}");
        return (link.Groups[2].Value.Trim(), link.Groups[1].Value);
    }

    private static string InputValue(string form, string name) =>
        AvailabilityPages.Text(form, $@"<input [^>]*name=""{name}""[^>]*value=""([^""]*)""");

    /// <summary>The label of the one option card whose radio is checked.</summary>
    private static string ChosenOption(string form, string optionDataTest)
    {
        var chosen = form.Split($"data-test=\"{optionDataTest}\"").Skip(1)
            .Where(option => Regex.IsMatch(option[..option.IndexOf("</label>", StringComparison.Ordinal)], @"type=""radio""[^>]*checked=""checked"""))
            .Select(option => AvailabilityPages.Text(option, @"class=""option-card__label"">([^<]*)</span>"))
            .ToList();
        return Assert.Single(chosen);
    }

    /// <summary>The markup from <paramref name="from"/> up to the first <paramref name="to"/> after it.</summary>
    private static string Between(string page, string from, string to)
    {
        var section = page[page.IndexOf(from, StringComparison.Ordinal)..];
        return section[..section.IndexOf(to, StringComparison.Ordinal)];
    }

    private static IReadOnlyList<string> Paragraphs(string markup) =>
        Regex.Matches(markup, @"<p>([^<]*)</p>").Select(match => WebUtility.HtmlDecode(match.Groups[1].Value).Trim()).ToList();

    private static string AmountLine(string markup, string label) =>
        AvailabilityPages.Text(markup, $@"class=""order-summary__line""><span>{Regex.Escape(label)}</span><span>([^<]*)</span>");

    private static string Total(string summary) =>
        AvailabilityPages.Text(summary, @"class=""order-summary__total""><span>Total</span><span data-test=""order-summary-total"">([^<]*)</span>");

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}