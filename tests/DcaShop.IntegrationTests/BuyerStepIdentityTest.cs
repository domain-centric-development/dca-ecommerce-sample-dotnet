using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The buyer step knows who is buying: a guest is offered a login or a registration, a logged-in customer finds the
/// account's email filled in and orders with it — through the real HTTP pipeline. One client is one browser;
/// redirects are not followed, each one is an assertion.
/// </summary>
public sealed class BuyerStepIdentityTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Secret123";
    private const string DomainDrivenDesign = "Domain-Driven Design";

    private readonly WebApplicationFactory<Program> _factory;

    public BuyerStepIdentityTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task AGuestOnTheBuyerStepIsOfferedToLogInOrRegisterAndFindsTheEmailEmpty()
    {
        var guest = NewBrowser();
        await AddOneUnitAsync(guest, DomainDrivenDesign);
        Assert.Equal("/checkout/buyer", await StartCheckoutAsync(guest));

        var buyerStep = await guest.GetStringAsync("/checkout/buyer");

        var prompt = AvailabilityPages.Section(buyerStep, "buyer-auth-options");
        Assert.Equal("Already have an account? Log in to speed up checkout.", AvailabilityPages.Text(prompt, @"<p>([^<]*)</p>"));
        Assert.Equal(("Login", "/login?returnUrl=/checkout/buyer"), Link(prompt, "buyer-login-link"));
        Assert.Equal(("Register", "/register?returnUrl=/checkout/buyer"), Link(prompt, "buyer-register-link"));
        Assert.Equal(string.Empty, EmailValue(buyerStep));
    }

    [Fact]
    public async Task ALoggedInCustomerWhoKeepsTheFilledInEmailOrdersWithTheAccountsEmail()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewBrowser(), email, returnUrl: null);
        var customer = NewBrowser();
        await LogInAsync(customer, email);
        await AddOneUnitAsync(customer, DomainDrivenDesign);
        Assert.Equal("/checkout/buyer", await StartCheckoutAsync(customer));
        await CompleteTheStepsKeepingTheFilledInEmailAsync(customer);

        var confirmation = await PlaceTheOrderAsync(customer);

        Assert.Equal("Your order has been placed successfully", AvailabilityPages.Text(confirmation, @"data-test=""confirmation-title""[^>]*>([^<]*)<"));
        Assert.Contains(email, Paragraphs(DetailSection(confirmation, "Contact Information")));
    }

    [Fact]
    public async Task AGuestWhoRegistersFromTheBuyerStepPlacesTheOrderLoggedIn()
    {
        var email = UniqueEmail();
        var guest = NewBrowser();
        await AddOneUnitAsync(guest, DomainDrivenDesign);
        Assert.Equal("/checkout/buyer", await StartCheckoutAsync(guest));
        var (_, registerTarget) = Link(AvailabilityPages.Section(await guest.GetStringAsync("/checkout/buyer"), "buyer-auth-options"), "buyer-register-link");
        Assert.Equal("/checkout/buyer", await RegisterAsync(guest, email, returnUrl: registerTarget["/register?returnUrl=".Length..]));
        await CompleteTheStepsKeepingTheFilledInEmailAsync(guest);

        var confirmation = await PlaceTheOrderAsync(guest);

        Assert.Equal("Your order has been placed successfully", AvailabilityPages.Text(confirmation, @"data-test=""confirmation-title""[^>]*>([^<]*)<"));
        Assert.Contains(email, Paragraphs(DetailSection(confirmation, "Contact Information")));
        Assert.Equal($"Welcome, {email}", AvailabilityPages.Text(confirmation, @"data-test=""user-greeting"">([^<]*)</span>"));
    }

    private static string UniqueEmail() => $"anna.schmidt-{Guid.NewGuid():N}@example.com";

    private HttpClient NewBrowser() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    /// <summary>Registers as Anna Schmidt on the registration page; answers where the visitor lands.</summary>
    private static async Task<string> RegisterAsync(HttpClient browser, string email, string? returnUrl)
    {
        var page = await browser.GetStringAsync(returnUrl is null ? "/register" : $"/register?returnUrl={returnUrl}");
        var registered = await browser.PostAsync(
            "/register",
            Form(
                page,
                ("email", email),
                ("password", Password),
                ("confirmPassword", Password),
                ("firstName", "Anna"),
                ("lastName", "Schmidt"),
                ("dateOfBirth", "1990-05-17"),
                ("returnUrl", returnUrl ?? string.Empty)));
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);
        return registered.Headers.Location!.ToString();
    }

    /// <summary>Logs in on the login page and follows the cart's merge page.</summary>
    private static async Task LogInAsync(HttpClient browser, string email)
    {
        var page = await browser.GetStringAsync("/login");
        var loggedIn = await browser.PostAsync("/login", Form(page, ("email", email), ("password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);
        var merged = await browser.GetAsync(loggedIn.Headers.Location);
        Assert.Equal(HttpStatusCode.Redirect, merged.StatusCode);
    }

    private static async Task AddOneUnitAsync(HttpClient browser, string productName)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(browser, productName);
        var productPage = await browser.GetStringAsync(productPath);
        var added = await browser.PostAsync(
            "/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", "1")));
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
    }

    /// <summary>Presses "Proceed to Checkout" on the cart page; answers where it leads.</summary>
    private static async Task<string> StartCheckoutAsync(HttpClient browser)
    {
        var cartPage = await browser.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await browser.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);
        return started.Headers.Location!.ToString();
    }

    /// <summary>Submits the buyer step with the email it was rendered with, then the delivery and payment steps.</summary>
    private static async Task CompleteTheStepsKeepingTheFilledInEmailAsync(HttpClient browser)
    {
        var buyerStep = await browser.GetStringAsync("/checkout/buyer");
        await Step(browser, buyerStep, "/checkout/buyer", "/checkout/delivery",
            ("email", EmailValue(buyerStep)), ("firstName", "Anna"), ("lastName", "Schmidt"), ("phone", "+49 30 1234567"));
        await Step(browser, await browser.GetStringAsync("/checkout/delivery"), "/checkout/delivery", "/checkout/payment",
            ("street", "Hauptstrasse 1"), ("streetLine2", string.Empty), ("city", "Berlin"), ("state", string.Empty),
            ("postalCode", "10115"), ("country", "Germany"), ("shippingOptionId", "standard"));
        await Step(browser, await browser.GetStringAsync("/checkout/payment"), "/checkout/payment", "/checkout/review", ("providerId", "mock"));
    }

    /// <summary>Presses "Place Order" on the review step; answers the confirmation page.</summary>
    private static async Task<string> PlaceTheOrderAsync(HttpClient browser)
    {
        await Step(browser, await browser.GetStringAsync("/checkout/review"), "/checkout/confirm", "/checkout/confirmation", ("placedFrom", "review"));
        return await browser.GetStringAsync("/checkout/confirmation");
    }

    private static async Task Step(HttpClient browser, string renderedPage, string url, string expectedNext, params (string, string)[] fields)
    {
        var response = await browser.PostAsync(url, Form(renderedPage, fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedNext, response.Headers.Location!.ToString());
    }

    /// <summary>The value the buyer step's "Email Address" field is rendered with.</summary>
    private static string EmailValue(string buyerStep) =>
        WebUtility.HtmlDecode(AvailabilityPages.Text(buyerStep, @"<input [^>]*\bvalue=""([^""]*)"" data-test=""buyer-email-input"">"));

    /// <summary>The text and target of the link carrying <paramref name="dataTest"/>.</summary>
    private static (string Text, string Target) Link(string markup, string dataTest)
    {
        var link = Regex.Match(markup, $@"<a [^>]*href=""([^""]*)"" data-test=""{dataTest}"">([^<]*)</a>");
        Assert.True(link.Success, $"the markup has a {dataTest}");
        return (link.Groups[2].Value.Trim(), WebUtility.HtmlDecode(link.Groups[1].Value));
    }

    /// <summary>A confirmation detail block: what stands below its heading, up to the next heading or the block's end.</summary>
    private static string DetailSection(string page, string heading)
    {
        var section = page[(page.IndexOf($"<h4>{heading}</h4>", StringComparison.Ordinal) + heading.Length + 9)..];
        var end = new[] { section.IndexOf("<h4>", StringComparison.Ordinal), section.IndexOf("</div>", StringComparison.Ordinal) }.Where(i => i >= 0).Min();
        return section[..end];
    }

    private static IReadOnlyList<string> Paragraphs(string markup) =>
        Regex.Matches(markup, @"<p>([^<]*)</p>").Select(match => WebUtility.HtmlDecode(match.Groups[1].Value).Trim()).ToList();

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}