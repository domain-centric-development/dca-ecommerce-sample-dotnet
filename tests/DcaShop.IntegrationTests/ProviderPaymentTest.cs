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
/// The payment step against the payment provider, reached over HTTP: the shop is started with the provider's
/// address, and the provider is a stub on a free port that answers what the test arranges.
/// </summary>
public sealed class ProviderPaymentTest : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string RefusalText = "The payment was refused. Please choose another way to pay.";
    private const string UnavailableText = "The payment provider is not available right now. Please try again later.";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly WireMockServer _provider = WireMockServer.Start();

    public ProviderPaymentTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ARefusedPaymentKeepsTheCustomerAtThePaymentStepWithTheRefusalText()
    {
        ProviderAnswers(Response.Create().WithStatusCode(402));
        var client = await ClientAtThePaymentStepAsync();

        var response = await PayAsync(client);

        await AssertStaysAtPaymentShowingAsync(response, RefusalText);
    }

    [Fact]
    public async Task AProviderThatAnswersOnlyAfterFiveSecondsCountsAsUnavailable()
    {
        ProviderAnswers(Response.Create().WithStatusCode(201).WithBodyAsJson(new { reference = "late" }).WithDelay(TimeSpan.FromSeconds(5)));
        var client = await ClientAtThePaymentStepAsync();

        var watch = Stopwatch.StartNew();
        var response = await PayAsync(client);
        watch.Stop();

        await AssertStaysAtPaymentShowingAsync(response, UnavailableText);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(4), $"the late answer was awaited ({watch.Elapsed.TotalSeconds:0.0} s) instead of being abandoned at the timeout");
    }

    [Fact]
    public async Task AnAuthorizedPaymentMovesOnToTheReviewAfterOneRequestForTheSessionTotal()
    {
        ProviderAnswers(Response.Create().WithStatusCode(201).WithBodyAsJson(new { reference = "pay-4711" }));
        var client = await ClientAtThePaymentStepAsync();

        var response = await PayAsync(client);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/checkout/review", response.Headers.Location!.ToString());
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
        var client = await ClientAtThePaymentStepAsync();

        var response = await PayAsync(client);

        await AssertStaysAtPaymentShowingAsync(response, UnavailableText);
    }

    [Fact]
    public async Task AnAuthorizationWithoutAPaymentReferenceCountsAsUnavailable()
    {
        ProviderAnswers(Response.Create().WithStatusCode(201).WithBody("{}", encoding: System.Text.Encoding.UTF8).WithHeader("Content-Type", "application/json"));
        var client = await ClientAtThePaymentStepAsync();

        var response = await PayAsync(client);

        await AssertStaysAtPaymentShowingAsync(response, UnavailableText);
    }

    [Fact]
    public async Task AProviderThatCannotBeReachedCountsAsUnavailable()
    {
        _provider.Stop();
        var client = await ClientAtThePaymentStepAsync();

        var response = await PayAsync(client);

        await AssertStaysAtPaymentShowingAsync(response, UnavailableText);
    }

    private void ProviderAnswers(IResponseBuilder answer) =>
        _provider.Given(Request.Create().WithPath("/payments").UsingPost()).RespondWith(answer);

    /// <summary>The shop as it runs with a payment provider configured — this test's stub.</summary>
    private WebApplicationFactory<Program> ShopWithTheProvider() =>
        _factory.WithWebHostBuilder(builder => builder.UseSetting("Checkout:PaymentProvider:BaseUrl", _provider.Url));

    /// <summary>
    /// A visitor whose checkout is at the payment step: one Hexagon Sticker Sheet (9.99 EUR) with Free Shipping,
    /// so the session total is 9.99 EUR.
    /// </summary>
    private async Task<HttpClient> ClientAtThePaymentStepAsync()
    {
        var client = ShopWithTheProvider().CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

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

        return client;
    }

    /// <summary>The customer pays with the provider the payment page offers.</summary>
    private static async Task<HttpResponseMessage> PayAsync(HttpClient client)
    {
        var paymentPage = await client.GetStringAsync("/checkout/payment");
        var providerId = Regex.Match(paymentPage, @"name=""providerId"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(providerId);

        return await client.PostAsync("/checkout/payment", Form(paymentPage, ("providerId", providerId)));
    }

    /// <summary>Stays at the payment step: the payment page itself answers, not a redirect, with the text in its error slot.</summary>
    private static async Task AssertStaysAtPaymentShowingAsync(HttpResponseMessage response, string text)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-test=\"payment-form\"", page, StringComparison.Ordinal);
        var error = Regex.Match(page, @"data-test=""payment-error-message"">([^<]*)<").Groups[1].Value;
        Assert.Equal(text, WebUtility.HtmlDecode(error));
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