using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The cart follows the person, not the browser, through registration, logout and a lapsed login — through the real
/// HTTP pipeline. One client is one browser; redirects are not followed, each one is an assertion.
/// </summary>
public sealed class CartStaysWithThePersonTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string IdentityCookie = "shop-identity";
    private const string Password = "Secret123";
    private const string CleanArchitecture = "Clean Architecture";
    private const string DomainDrivenDesign = "Domain-Driven Design";
    private const string Mug = "\"Ubiquitous Language\" Mug";

    private static readonly Regex CartItem = new(
        @"data-test=""cart-item-name"">([^<]*)</div>.*?data-test=""cart-item-quantity"">([^<]*)</span>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly WebApplicationFactory<Program> _factory;

    public CartStaysWithThePersonTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task RegisteringFromTheCheckoutReturnsToTheBuyerStepWithTheGuestsOrder()
    {
        var (guest, _) = NewBrowser();
        await AddOneUnitAsync(guest, DomainDrivenDesign);
        Assert.Equal("/checkout/buyer", await StartCheckoutAsync(guest));

        var landing = await RegisterAsync(guest, UniqueEmail(), returnUrl: "/checkout/buyer");
        var buyerStep = await guest.GetAsync(landing);

        Assert.Equal("/checkout/buyer", landing);
        Assert.Equal(HttpStatusCode.OK, buyerStep.StatusCode);
        var summary = AvailabilityPages.Section(await buyerStep.Content.ReadAsStringAsync(), "order-summary-item");
        Assert.Equal([$"{DomainDrivenDesign} x1"], SummaryItems(summary));
    }

    [Fact]
    public async Task ACustomerWhoJustRegisteredAddsToTheirCart()
    {
        var (browser, _) = NewBrowser();
        await RegisterAsync(browser, UniqueEmail(), returnUrl: null);

        await AddOneUnitAsync(browser, CleanArchitecture);

        Assert.Equal([$"{CleanArchitecture} x1"], CartLines(await browser.GetStringAsync("/cart")));
    }

    [Fact]
    public async Task TheCartTakenOverAtRegistrationComesBackAtTheNextLogin()
    {
        var email = UniqueEmail();
        var (browser, _) = NewBrowser();
        await AddOneUnitAsync(browser, DomainDrivenDesign);
        await RegisterAsync(browser, email, returnUrl: null);
        await LogOutAsync(browser);

        var landing = await LogInAsync(browser, email);

        Assert.Equal("/cart", landing);
        Assert.Equal([$"{DomainDrivenDesign} x1"], CartLines(await browser.GetStringAsync("/cart")));
    }

    [Fact]
    public async Task LoggingOutLeavesTheBrowserWithAnEmptyCart()
    {
        var (browser, _) = NewBrowser();
        await RegisterAsync(browser, UniqueEmail(), returnUrl: null);
        await AddOneUnitAsync(browser, DomainDrivenDesign);
        await LogOutAsync(browser);

        var cartPage = await browser.GetStringAsync("/cart");

        Assert.Equal("Your cart is empty", AvailabilityPages.Text(cartPage, @"<h3 class=""empty-state__title"">([^<]*)</h3>"));
    }

    [Fact]
    public async Task ALapsedLoginOffersTheLoginAgainAndKeepsTheCart()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewBrowser().Browser, email, returnUrl: null);
        var (browser, cookies) = NewBrowser();
        await AddOneUnitAsync(browser, Mug);
        await LogInTakingTheCartOverAsync(browser, email);

        // The session cookie aged out after its 7 days; the identity cookie, good for 30, is all the browser still sends.
        var lapsed = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        lapsed.DefaultRequestHeaders.Add("Cookie", $"{IdentityCookie}={cookies.GetCookies(new Uri("http://localhost"))[IdentityCookie]!.Value}");
        var cartPage = await lapsed.GetStringAsync("/cart");

        Assert.DoesNotContain("data-test=\"user-greeting\"", cartPage, StringComparison.Ordinal);
        Assert.Equal("Login", AvailabilityPages.Text(cartPage, @"data-test=""nav-login-link"">([^<]*)</a>"));
        Assert.Equal("Register", AvailabilityPages.Text(cartPage, @"data-test=""nav-register-link"">([^<]*)</a>"));
        Assert.Equal([$"{Mug} x1"], CartLines(cartPage));
    }

    private static string UniqueEmail() => $"anna.schmidt-{Guid.NewGuid():N}@example.com";

    /// <summary>A browser of its own whose cookies the test can read — the lapsed login needs the identity cookie.</summary>
    private (HttpClient Browser, CookieContainer Cookies) NewBrowser()
    {
        var cookies = new CookieContainer();
        return (_factory.CreateDefaultClient(new CookieContainerHandler(cookies)), cookies);
    }

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

    /// <summary>Logs in on the login page and follows the cart's merge page; answers where the visitor lands.</summary>
    private static async Task<string> LogInAsync(HttpClient browser, string email)
    {
        var page = await browser.GetStringAsync("/login");
        var loggedIn = await browser.PostAsync("/login", Form(page, ("email", email), ("password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);
        Assert.StartsWith("/cart/merge", loggedIn.Headers.Location!.ToString(), StringComparison.Ordinal);

        var merged = await browser.GetAsync(loggedIn.Headers.Location);
        Assert.Equal(HttpStatusCode.Redirect, merged.StatusCode);
        return merged.Headers.Location!.ToString();
    }

    /// <summary>
    /// Logs in on the login page with a guest cart the account takes over: the page after the login opens and its form
    /// is sent.
    /// </summary>
    private static async Task LogInTakingTheCartOverAsync(HttpClient browser, string email)
    {
        var page = await browser.GetStringAsync("/login");
        var loggedIn = await browser.PostAsync("/login", Form(page, ("email", email), ("password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);

        var takeOverPage = await browser.GetAsync(loggedIn.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, takeOverPage.StatusCode);
        var takenOver = await TakeOverForm.SendAsync(browser, await takeOverPage.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Redirect, takenOver.StatusCode);
    }

    /// <summary>Presses "Logout" in the header of the cart page.</summary>
    private static async Task LogOutAsync(HttpClient browser)
    {
        var page = await browser.GetStringAsync("/cart");
        var loggedOut = await browser.PostAsync("/logout", Form(page));
        Assert.Equal(HttpStatusCode.Redirect, loggedOut.StatusCode);
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

    /// <summary>The order summary's lines as <c>name xquantity</c>, in page order.</summary>
    private static IReadOnlyList<string> SummaryItems(string summary) =>
        summary.Split("data-test=\"order-summary-item\"").Skip(1)
            .Select(entry =>
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-name"">([^<]*)</span>") + " " +
                AvailabilityPages.Text(entry, @"data-test=""order-summary-item-qty"">([^<]*)</span>"))
            .ToList();

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}