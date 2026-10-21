using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DcaShop.IntegrationTests;

/// <summary>
/// "Place Order" against the payment provider, reached over HTTP: the shop is started with the provider's address, and
/// the provider is a stub on a free port that answers what the test arranges. Every answer but an authorization keeps
/// the customer at the review with the shop's own sentence.
/// </summary>
public sealed class ProviderPaymentTest : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string RefusalText = "The payment was refused. Please choose another way to pay.";
    private const string UnavailableText = "The payment provider is not available right now. Please try again later.";

    /// <summary>
    /// How long the shop waits for the provider in every case but the slow one: generous, so an answer the stub gives
    /// at once still arrives in time when the machine is busy running every suite.
    /// </summary>
    private const string GenerousTimeout = "00:00:30";

    /// <summary>The shop's own timeout, for the case that is about the timeout.</summary>
    private const string ShortTimeout = "00:00:02";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly WireMockServer _provider = WireMockServer.Start();

    public ProviderPaymentTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ARefusedPaymentKeepsTheCustomerAtTheReviewWithTheRefusalText()
    {
        ProviderAnswers(Response.Create().WithStatusCode(402));
        var client = await ClientAtTheReviewAsync();

        var response = await PlaceOrderAsync(client);

        await AssertStaysAtTheReviewShowingAsync(client, response, RefusalText);
    }

    [Fact]
    public async Task AProviderThatAnswersOnlyAfterFiveSecondsCountsAsUnavailable()
    {
        ProviderAnswers(Response.Create().WithStatusCode(201).WithBodyAsJson(new { reference = "late" }).WithDelay(TimeSpan.FromSeconds(5)));
        var client = await ClientAtTheReviewAsync(ShortTimeout);

        var watch = Stopwatch.StartNew();
        var response = await PlaceOrderAsync(client);
        watch.Stop();

        await AssertStaysAtTheReviewShowingAsync(client, response, UnavailableText);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(4), $"the late answer was awaited ({watch.Elapsed.TotalSeconds:0.0} s) instead of being abandoned at the timeout");
    }

    [Fact]
    public async Task AnAuthorizedPaymentPlacesTheOrderAfterOneRequestForTheSessionTotal()
    {
        ProviderAnswers(Response.Create().WithStatusCode(201).WithBodyAsJson(new { reference = "pay-4711" }));
        var client = await ClientAtTheReviewAsync();

        var response = await PlaceOrderAsync(client);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/checkout/confirmation", response.Headers.Location!.ToString());
        var request = Assert.Single(_provider.LogEntries).RequestMessage!;
        Assert.Equal("POST", request.Method);
        Assert.Equal("/payments", request.Path);
        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal("9.99", body.RootElement.GetProperty("amount").GetString());
        Assert.Equal("EUR", body.RootElement.GetProperty("currency").GetString());
    }

    [Fact]
    public async Task AProviderFailingWithAServerErrorCountsAsUnavailable()
    {
        ProviderAnswers(Response.Create().WithStatusCode(500));
        var client = await ClientAtTheReviewAsync();

        var response = await PlaceOrderAsync(client);

        await AssertStaysAtTheReviewShowingAsync(client, response, UnavailableText);
    }

    [Fact]
    public async Task AnAuthorizationWithoutAPaymentReferenceCountsAsUnavailable()
    {
        ProviderAnswers(Response.Create().WithStatusCode(201).WithBody("{}", encoding: System.Text.Encoding.UTF8).WithHeader("Content-Type", "application/json"));
        var client = await ClientAtTheReviewAsync();

        var response = await PlaceOrderAsync(client);

        await AssertStaysAtTheReviewShowingAsync(client, response, UnavailableText);
    }

    [Fact]
    public async Task AProviderThatCannotBeReachedCountsAsUnavailable()
    {
        _provider.Stop();
        var client = await ClientAtTheReviewAsync();

        var response = await PlaceOrderAsync(client);

        await AssertStaysAtTheReviewShowingAsync(client, response, UnavailableText);
    }

    private void ProviderAnswers(IResponseBuilder answer) =>
        _provider.Given(Request.Create().WithPath("/payments").UsingPost()).RespondWith(answer);

    /// <summary>The shop as it runs with a payment provider configured — this test's stub, awaited at most <paramref name="timeout"/>.</summary>
    private WebApplicationFactory<Program> ShopWithTheProvider(string timeout) =>
        _factory.WithWebHostBuilder(builder => builder
            .UseSetting("Checkout:PaymentProvider:BaseUrl", _provider.Url)
            .UseSetting("Checkout:PaymentProvider:Timeout", timeout));

    /// <summary>
    /// A visitor whose checkout is at the review: one Hexagon Sticker Sheet (9.99 EUR) with Free Shipping, so the
    /// session total is 9.99 EUR, and the provider the payment page offers chosen.
    /// </summary>
    private async Task<HttpClient> ClientAtTheReviewAsync(string providerTimeout = GenerousTimeout)
    {
        var client = ShopWithTheProvider(providerTimeout).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        var products = await client.GetFromJsonAsync<JsonElement>("/api/products");
        var productId = products.EnumerateArray().First(p => p.GetProperty("sku").GetString() == "STICKER-001").GetProperty("id").GetString()!;

        var detail = await client.GetStringAsync($"/products/{productId}");
        var added = await client.PostAsync("/cart/add-product", Form(detail, ("productId", productId), ("quantity", "1")));
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);

        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);

        await Step(client, "/checkout/buyer", "/checkout/delivery",
            ("email", "ada@example.com"), ("firstName", "Ada"), ("lastName", "Lovelace"), ("phone", "0123"));
        await Step(client, "/checkout/delivery", "/checkout/payment",
            ("street", "Analytical Engine Way 1"), ("city", "London"), ("postalCode", "12345"), ("country", "UK"), ("shippingOptionId", "free"));

        var paymentPage = await client.GetStringAsync("/checkout/payment");
        var providerId = Regex.Match(paymentPage, @"name=""providerId"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(providerId);
        await Step(client, "/checkout/payment", "/checkout/review", ("providerId", providerId));

        return client;
    }

    /// <summary>The customer presses "Place Order" on the review.</summary>
    private static async Task<HttpResponseMessage> PlaceOrderAsync(HttpClient client) =>
        await client.PostAsync("/checkout/confirm", Form(await client.GetStringAsync("/checkout/review"), ("placedFrom", "review")));

    /// <summary>Stays at the review: redirected to its own address, which shows the text in its error slot and no confirmation.</summary>
    private static async Task AssertStaysAtTheReviewShowingAsync(HttpClient client, HttpResponseMessage response, string text)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/checkout/review", response.Headers.Location!.ToString());
        var page = await client.GetStringAsync(response.Headers.Location);
        var error = Regex.Match(page, @"data-test=""review-error-message"">([^<]*)<").Groups[1].Value;
        Assert.Equal(text, WebUtility.HtmlDecode(error));
        Assert.DoesNotContain("data-test=\"confirmation-title\"", page, StringComparison.Ordinal);
    }

    private static async Task Step(HttpClient client, string url, string expectedNext, params (string, string)[] fields)
    {
        var response = await client.PostAsync(url, Form(await client.GetStringAsync(url), fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedNext, response.Headers.Location!.ToString());
    }

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }

    public void Dispose() => _provider.Stop();
}