using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The step pages show the checkout's progress, and a step ahead of it leads to the step that is open — through the
/// real HTTP pipeline. One cookie jar is one browser; redirects are not followed, each one is an assertion.
/// </summary>
public sealed class StepProgressTest : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Regex ProgressStep = new(
        @"<div class=""checkout__step(?: checkout__step--(?<state>completed|active))?""><div class=""checkout__step-marker"">(?<number>\d)</div><div class=""checkout__step-label"">(?<label>[^<]*)</div>",
        RegexOptions.Compiled);

    private readonly WebApplicationFactory<Program> _factory;

    public StepProgressTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheBuyerStepShowsFourNumberedStepsWithBuyerInfoCurrentAndNoneDone()
    {
        var guest = Browser();
        await GuestWhoJustStartedTheCheckoutAsync(guest);

        var progress = Progress(await guest.GetStringAsync("/checkout/buyer"));

        Assert.Equal(new[] { "1 Buyer Info", "2 Delivery", "3 Payment", "4 Review" }, progress.Select(s => $"{s.Number} {s.Label}"));
        Assert.Equal(new[] { "current", "open", "open", "open" }, progress.Select(s => s.State));
    }

    [Fact]
    public async Task ThePlacedOrderShowsAllFourStepsDoneEndingInConfirmed()
    {
        var guest = Browser();
        await GuestWhoJustStartedTheCheckoutAsync(guest);
        await SubmitBuyerAndDeliveryAsync(guest);
        await Step(guest, "/checkout/payment", "/checkout/review", "/checkout/payment", ("providerId", "mock"));

        await Step(guest, "/checkout/review", "/checkout/confirmation", "/checkout/confirm", ("placedFrom", "review"));

        var progress = Progress(await guest.GetStringAsync("/checkout/confirmation"));
        Assert.Equal(new[] { "Buyer Info done", "Delivery done", "Payment done", "Confirmed done" }, progress.Select(s => $"{s.Label} {s.State}"));
    }

    [Fact]
    public async Task TheReviewOpenedFromTheDeliveryStepOpensTheDeliveryStep()
    {
        var guest = Browser();
        await GuestWhoJustStartedTheCheckoutAsync(guest);
        await Step(guest, "/checkout/buyer", "/checkout/delivery", "/checkout/buyer", BuyerFields);

        var response = await guest.GetAsync("/checkout/review");

        Assert.Equal("/checkout/delivery", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task ThePaymentStepOpenedFromTheBuyerStepOpensTheBuyerStep()
    {
        var guest = Browser();
        await GuestWhoJustStartedTheCheckoutAsync(guest);

        var response = await guest.GetAsync("/checkout/payment");

        Assert.Equal("/checkout/buyer", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task ADeliveryFormSentBeforeTheBuyerStepLeadsToTheBuyerStepAndKeepsTheReviewClosed()
    {
        var guest = Browser();
        await GuestWhoJustStartedTheCheckoutAsync(guest);

        var sent = await guest.PostAsync("/checkout/delivery", Form(await guest.GetStringAsync("/checkout/buyer"), DeliveryFields));

        Assert.Equal("/checkout/buyer", sent.Headers.Location!.OriginalString.Split('?')[0]);
        var review = await guest.GetAsync("/checkout/review");
        Assert.Equal("/checkout/buyer", review.Headers.Location!.ToString());
    }

    private static readonly (string, string)[] BuyerFields =
        [("email", "jane@example.com"), ("firstName", "Jane"), ("lastName", "Doe"), ("phone", "+49 30 1234567")];

    private static readonly (string, string)[] DeliveryFields =
        [("street", "Hauptstrasse 1"), ("streetLine2", string.Empty), ("city", "Berlin"), ("state", string.Empty),
            ("postalCode", "10115"), ("country", "Germany"), ("shippingOptionId", "standard")];

    private HttpClient Browser() => _factory.CreateDefaultClient(new CookieContainerHandler(new CookieContainer()));

    /// <summary>The steps of the page's progress bar in their order: number, label and whether done, current or open.</summary>
    private static IReadOnlyList<(string Number, string Label, string State)> Progress(string page) =>
        ProgressStep.Matches(AvailabilityPages.Section(page, "checkout-progress"))
            .Select(m => (m.Groups["number"].Value, m.Groups["label"].Value, m.Groups["state"].Value switch
            {
                "completed" => "done",
                "active" => "current",
                _ => "open",
            }))
            .ToList();

    /// <summary>Puts "Domain-Driven Design" into the cart and starts the checkout.</summary>
    private static async Task GuestWhoJustStartedTheCheckoutAsync(HttpClient client)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(client, "Domain-Driven Design");
        var productPage = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", "1")));
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal("/checkout/buyer", started.Headers.Location!.ToString());
    }

    private static async Task SubmitBuyerAndDeliveryAsync(HttpClient client)
    {
        await Step(client, "/checkout/buyer", "/checkout/delivery", "/checkout/buyer", BuyerFields);
        await Step(client, "/checkout/delivery", "/checkout/payment", "/checkout/delivery", DeliveryFields);
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