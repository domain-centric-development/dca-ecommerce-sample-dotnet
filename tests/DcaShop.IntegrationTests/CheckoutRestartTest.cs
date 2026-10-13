using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>Starting the checkout again replaces the open one with the current cart, through the real HTTP pipeline.</summary>
public sealed class CheckoutRestartTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Pin = "\"Bounded Context\" Enamel Pin";
    private const string Hoodie = "\"Domain over Framework\" Hoodie";

    private static readonly Regex Field = new(@"<label class=""form__label"" for=""\w+"">([^<]*)</label>\s*<input ([^>]*)>", RegexOptions.Compiled);

    private readonly WebApplicationFactory<Program> _factory;

    public CheckoutRestartTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProceedingAgainShowsTheCurrentCartAndItsSubtotalOnTheBuyerStep()
    {
        var client = Client();
        await AddUnitsAsync(client, Pin, 2);
        await StartCheckoutAsync(client);
        await AddUnitsAsync(client, Pin, 3);
        await AddUnitsAsync(client, Hoodie, 1);

        var buyerStep = await StartCheckoutAsync(client);

        var summary = AvailabilityPages.Section(buyerStep, "order-summary-item");
        Assert.Equal(new[] { Pin + " x5", Hoodie + " x1" }, SummaryItems(summary));
        Assert.Equal("124.94 EUR", AvailabilityPages.Text(summary, @"class=""order-summary__total""><span>Subtotal</span><span>([^<]*)</span>"));
    }

    [Fact]
    public async Task ProceedingAgainOpensTheBuyerStepWithAllFieldsEmpty()
    {
        var client = Client();
        await AddUnitsAsync(client, Pin, 1);
        await StartCheckoutAsync(client);
        var buyerStep = await client.GetStringAsync("/checkout/buyer");
        var submitted = await client.PostAsync("/checkout/buyer", Form(buyerStep,
            ("email", "jane@example.com"), ("firstName", "Jane"), ("lastName", "Doe"), ("phone", "+49 30 1234567")));
        Assert.Equal("/checkout/delivery", submitted.Headers.Location!.ToString());

        var restarted = await StartCheckoutAsync(client);

        Assert.Equal(
            new[] { ("Email Address", ""), ("First Name", ""), ("Last Name", ""), ("Phone Number", "") },
            FieldValues(AvailabilityPages.Section(restarted, "buyer-form")));
    }

    private HttpClient Client() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    /// <summary>Presses "Proceed to Checkout" on the cart page and answers the buyer step it leads to.</summary>
    private static async Task<string> StartCheckoutAsync(HttpClient client)
    {
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);
        Assert.Equal("/checkout/buyer", started.Headers.Location!.ToString());
        return await client.GetStringAsync("/checkout/buyer");
    }

    private static async Task AddUnitsAsync(HttpClient client, string productName, int quantity)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(client, productName);
        var productPage = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", quantity.ToString())));
    }

    private static IReadOnlyList<string> SummaryItems(string summary) =>
        summary.Split("data-test=\"order-summary-item\"").Skip(1)
            .Select(entry =>
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-name"">([^<]*)</span>") + " " +
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-qty"">([^<]*)</span>"))
            .ToList();

    private static IReadOnlyList<(string Label, string Value)> FieldValues(string form) =>
        Field.Matches(form)
            .Select(match => (match.Groups[1].Value, Regex.Match(match.Groups[2].Value, @"\bvalue=""([^""]*)""").Groups[1].Value))
            .ToList();

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}