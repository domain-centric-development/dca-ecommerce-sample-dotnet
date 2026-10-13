using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Logging in to an existing account through the real HTTP pipeline: the account's cart is shown when the guest
/// collected nothing, a guest's open checkout stays behind with the guest, and the customer checks out like a guest.
/// One client is one browser; redirects are not followed, each one is an assertion.
/// </summary>
public sealed class CartRecoveryOnLoginTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Secret123";
    private const string CleanArchitecture = "Clean Architecture";
    private const string DomainDrivenDesign = "Domain-Driven Design";

    private static readonly Regex CartItem = new(
        @"data-test=""cart-item-name"">([^<]*)</div>.*?data-test=""cart-item-quantity"">([^<]*)</span>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly WebApplicationFactory<Program> _factory;

    public CartRecoveryOnLoginTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task AVisitorWithAnEmptyCartFindsTheAccountsCartAfterLoggingIn()
    {
        var email = UniqueEmail();
        var otherBrowser = NewBrowser();
        await RegisterAsync(otherBrowser, email);
        await AddOneUnitAsync(otherBrowser, CleanArchitecture);
        var browser = NewBrowser();

        var landing = await LogInAsync(browser, email, returnUrl: null);

        Assert.Equal("/cart", landing);
        Assert.Equal([$"{CleanArchitecture} x1"], CartLines(await browser.GetStringAsync("/cart")));
    }

    [Fact]
    public async Task LoggingInFromTheCheckoutBringsTheCartButNotTheGuestsCheckout()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewBrowser(), email);
        var guest = NewBrowser();
        await AddOneUnitAsync(guest, DomainDrivenDesign);
        Assert.Equal("/checkout/buyer", await StartCheckoutAsync(guest));
        await guest.GetStringAsync("/login?returnUrl=/checkout/buyer");

        var landing = await LogInAsync(guest, email, returnUrl: "/checkout/buyer");
        var buyerStep = await guest.GetAsync(landing);

        Assert.Equal("/checkout/buyer", landing);
        Assert.Equal("/cart", buyerStep.Headers.Location!.ToString());
        var cartPage = await guest.GetStringAsync("/cart");
        Assert.Equal("No active checkout session found", AvailabilityPages.Text(cartPage, @"data-test=""cart-error-message"">([^<]*)</div>"));
        Assert.Equal([$"{DomainDrivenDesign} x1"], CartLines(cartPage));
    }

    [Fact]
    public async Task ALoggedInCustomerStartsTheCheckoutAtTheBuyerStep()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewBrowser(), email);
        var browser = NewBrowser();
        await LogInAsync(browser, email, returnUrl: null);
        await AddOneUnitAsync(browser, DomainDrivenDesign);

        var next = await StartCheckoutAsync(browser);

        Assert.Equal("/checkout/buyer", next);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync(next)).StatusCode);
    }

    private static string UniqueEmail() => $"anna.schmidt-{Guid.NewGuid():N}@example.com";

    private HttpClient NewBrowser() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task RegisterAsync(HttpClient browser, string email)
    {
        var page = await browser.GetStringAsync("/register");
        var registered = await browser.PostAsync(
            "/register",
            Form(
                page,
                ("email", email),
                ("password", Password),
                ("confirmPassword", Password),
                ("firstName", "Anna"),
                ("lastName", "Schmidt"),
                ("dateOfBirth", "1990-05-17")));
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);
    }

    /// <summary>Logs in on the login page and follows the cart's merge page; answers where the visitor lands.</summary>
    private static async Task<string> LogInAsync(HttpClient browser, string email, string? returnUrl)
    {
        var page = await browser.GetStringAsync("/login");
        var loggedIn = await browser.PostAsync(
            "/login", Form(page, ("email", email), ("password", Password), ("returnUrl", returnUrl ?? string.Empty)));
        Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);
        Assert.StartsWith("/cart/merge", loggedIn.Headers.Location!.ToString(), StringComparison.Ordinal);

        var merged = await browser.GetAsync(loggedIn.Headers.Location);
        Assert.Equal(HttpStatusCode.Redirect, merged.StatusCode);
        return merged.Headers.Location!.ToString();
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

    /// <summary>The cart page's lines as <c>name xquantity</c>, in page order.</summary>
    private static IReadOnlyList<string> CartLines(string cartPage) =>
        CartItem.Matches(cartPage)
            .Select(m => $"{WebUtility.HtmlDecode(m.Groups[1].Value).Trim()} x{m.Groups[2].Value.Trim()}")
            .ToList();

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}