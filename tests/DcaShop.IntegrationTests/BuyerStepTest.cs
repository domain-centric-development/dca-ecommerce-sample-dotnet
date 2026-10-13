using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>The buyer step asks a guest for their details and refuses what it cannot take, through the real HTTP pipeline.</summary>
public sealed class BuyerStepTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Refusal = "Please check the details you entered.";

    private static readonly Regex Field = new(@"<label class=""form__label"" for=""\w+"">([^<]*)</label>\s*<input ([^>]*)>", RegexOptions.Compiled);

    private readonly WebApplicationFactory<Program> _factory;

    public BuyerStepTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheBuyerStepAsksForEmailNamesAndPhoneWithEmptyRequiredFields()
    {
        var client = await GuestWhoStartedTheCheckoutAsync();

        var buyerStep = await client.GetStringAsync("/checkout/buyer");

        Assert.Equal("Buyer Information", Heading(buyerStep));
        Assert.Equal(
            new FormField[]
            {
                new("Email Address", Required: true, Value: string.Empty),
                new("First Name", Required: true, Value: string.Empty),
                new("Last Name", Required: true, Value: string.Empty),
                new("Phone Number", Required: true, Value: string.Empty),
            },
            Fields(AvailabilityPages.Section(buyerStep, "buyer-form")));
        Assert.Equal("Continue to Delivery", AvailabilityPages.Text(buyerStep, @"data-test=""buyer-continue-button"">([^<]*)</button>"));
    }

    [Fact]
    public async Task AnEmailWithoutAnAtSignOpensTheBuyerStepAgainWithTheRefusal()
    {
        var client = await GuestWhoStartedTheCheckoutAsync();

        var response = await SubmitAsync(client, "jane.example.com", "Jane", "Doe", "+49 30 1234567");

        await AssertBuyerStepOpensAgainWithTheRefusalAsync(response);
    }

    [Fact]
    public async Task AFirstNameOfSpacesOnlyOpensTheBuyerStepAgainWithTheRefusal()
    {
        var client = await GuestWhoStartedTheCheckoutAsync();

        var response = await SubmitAsync(client, "jane@example.com", "   ", "Doe", "+49 30 1234567");

        await AssertBuyerStepOpensAgainWithTheRefusalAsync(response);
    }

    [Fact]
    public async Task APhoneNumberOfSpacesOnlyOpensTheBuyerStepAgainWithTheRefusal()
    {
        var client = await GuestWhoStartedTheCheckoutAsync();

        var response = await SubmitAsync(client, "jane@example.com", "Jane", "Doe", "   ");

        await AssertBuyerStepOpensAgainWithTheRefusalAsync(response);
    }

    private static async Task AssertBuyerStepOpensAgainWithTheRefusalAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Equal("Buyer Information", Heading(page));
        Assert.Equal(Refusal, AvailabilityPages.Text(page, @"data-test=""buyer-error-message"">([^<]*)</div>"));
    }

    private async Task<HttpClient> GuestWhoStartedTheCheckoutAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var productPath = await AvailabilityPages.ProductPathAsync(client, "Domain-Driven Design");
        var productPage = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", "1")));
        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal("/checkout/buyer", started.Headers.Location!.ToString());
        return client;
    }

    private static async Task<HttpResponseMessage> SubmitAsync(HttpClient client, string email, string firstName, string lastName, string phone)
    {
        var buyerStep = await client.GetStringAsync("/checkout/buyer");
        return await client.PostAsync("/checkout/buyer",
            Form(buyerStep, ("email", email), ("firstName", firstName), ("lastName", lastName), ("phone", phone)));
    }

    private static string Heading(string page) =>
        AvailabilityPages.Text(page, @"<h3 class=""checkout__section-title"">([^<]*)</h3>");

    private static IReadOnlyList<FormField> Fields(string form) =>
        Field.Matches(form)
            .Select(match => new FormField(
                match.Groups[1].Value,
                Regex.IsMatch(match.Groups[2].Value, @"\brequired\b"),
                Regex.Match(match.Groups[2].Value, @"\bvalue=""([^""]*)""").Groups[1].Value))
            .ToList();

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }

    private sealed record FormField(string Label, bool Required, string Value);
}