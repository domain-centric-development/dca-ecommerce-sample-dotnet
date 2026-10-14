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
    private const string Mug = "\"Ubiquitous Language\" Mug";

    private static readonly Regex CartItem = new(
        @"data-test=""cart-item-name"">([^<]*)</div>.*?data-test=""cart-item-quantity"">([^<]*)</span>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex MergeBoxFigure = new(
        @"<(?:h3 class=""merge-options__cart-title""|p|div class=""merge-options__cart-total"")>([^<]*)</",
        RegexOptions.Compiled);

    private static readonly Regex MergeBoxLine = new(
        @"class=""merge-options__item-image"">\s*(?:<img src=""([^""]*)""[^>]*>)?\s*</div>\s*"
        + @"<span class=""merge-options__item-name"">([^<]*)</span>\s*"
        + @"<span class=""merge-options__item-qty"">([^<]*)</span>\s*"
        + @"<span class=""merge-options__item-price"">([^<]*)</span>",
        RegexOptions.Compiled);

    private static readonly Regex MergeOptionCard = new(
        @"<input type=""radio"" name=""strategy"" value=""[^""]*""( checked(?:=""checked"")?)?>\s*"
        + @"<div class=""merge-options__card-title"">([^<]*)</div>\s*"
        + @"<div class=""merge-options__card-description"">([^<]*)</div>",
        RegexOptions.Compiled);

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
        var cartPage = await guest.GetStringAsync(buyerStep.Headers.Location);
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

    [Fact]
    public async Task LoggingInWithItemsInBothCartsAsksWhatToDoWithThem()
    {
        var (_, mergePage) = await LogInWithTwoCartsAsync();

        Assert.Equal("Your Cart Has Items", AvailabilityPages.Text(mergePage, @"class=""merge-options__title"">([^<]*)</h2>"));
        Assert.Equal(
            "You have items in both your current browsing session and your account. How would you like to proceed?",
            AvailabilityPages.Text(mergePage, @"class=""merge-options__intro"">([^<]*)</p>"));
    }

    [Fact]
    public async Task TheMergePageSummarisesBothCarts()
    {
        var (_, mergePage) = await LogInWithTwoCartsAsync();

        var guestBox = Between(mergePage, @"data-test=""anonymous-cart-summary""", @"data-test=""account-cart-summary""");
        Assert.Equal(
            ["Current Session Cart", "Items: 2", "Total Quantity: 3", "Total: 96.97 EUR"],
            CartBoxFigures(guestBox));
        Assert.Equal(
            [$"{CleanArchitecture}, Qty: 2, 39.99 EUR, with image", $"{Mug}, Qty: 1, 16.99 EUR, with image"],
            CartBoxLines(guestBox));
        var accountBox = Between(mergePage, @"data-test=""account-cart-summary""", @"class=""merge-options__choose-title""");
        Assert.Equal(["Account Cart", "Items: 1", "Total Quantity: 1", "Total: 39.99 EUR"], CartBoxFigures(accountBox));
        Assert.Equal([$"{CleanArchitecture}, Qty: 1, 39.99 EUR, with image"], CartBoxLines(accountBox));
    }

    [Fact]
    public async Task TheMergePageOffersThreeOptionsWithMergingBothSelected()
    {
        var (_, mergePage) = await LogInWithTwoCartsAsync();

        var choice = mergePage[mergePage.IndexOf(@"class=""merge-options__choose-title""", StringComparison.Ordinal)..];
        Assert.Equal("Choose an Option", AvailabilityPages.Text(choice, @"class=""merge-options__choose-title"">([^<]*)</h3>"));
        Assert.Equal(
            [
                "Merge Both Carts — Combine all items from both carts. If the same product exists in both, quantities will be added together. (selected)",
                "Use Account Cart Only — Keep your account cart and discard the items from your current session.",
                "Use Current Session Cart Only — Replace your account cart with the items from your current session.",
            ],
            MergeOptionCards(choice));
        Assert.Equal("Continue", AvailabilityPages.Text(choice, @"data-test=""merge-submit-button"">([^<]*)</button>"));
    }

    [Fact]
    public async Task UsingTheAccountCartOnlyDropsTheGuestLines()
    {
        var (guest, mergePage) = await LogInWithTwoCartsAsync();

        var landing = await ChooseAsync(guest, mergePage, "USE_ACCOUNT_CART");

        Assert.Equal("/cart", landing);
        var cartPage = await guest.GetStringAsync(landing);
        Assert.Equal("Using your account cart.", AvailabilityPages.Text(cartPage, @"data-test=""cart-success-message"">([^<]*)</div>"));
        Assert.Equal([$"{CleanArchitecture} x1"], CartLines(cartPage));
    }

    [Fact]
    public async Task UsingTheCurrentSessionCartOnlyReplacesTheAccountsLines()
    {
        var (guest, mergePage) = await LogInWithTwoCartsAsync();

        var landing = await ChooseAsync(guest, mergePage, "USE_ANONYMOUS_CART");

        Assert.Equal("/cart", landing);
        var cartPage = await guest.GetStringAsync(landing);
        Assert.Equal("Using your recent cart.", AvailabilityPages.Text(cartPage, @"data-test=""cart-success-message"">([^<]*)</div>"));
        Assert.Equal([$"{CleanArchitecture} x2", $"{Mug} x1"], CartLines(cartPage));
    }

    [Fact]
    public async Task AMergeOptionThePageDoesNotOfferIsRefused()
    {
        var (guest, mergePage) = await LogInWithTwoCartsAsync();

        var landing = await ChooseAsync(guest, mergePage, "KEEP_BOTH_APART");

        Assert.StartsWith("/cart/merge", landing, StringComparison.Ordinal);
        var refused = await guest.GetStringAsync(landing);
        Assert.Equal("Your Cart Has Items", AvailabilityPages.Text(refused, @"class=""merge-options__title"">([^<]*)</h2>"));
        Assert.Equal("Invalid merge option selected", AvailabilityPages.Text(refused, @"data-test=""merge-error-message"">([^<]*)</div>"));
    }

    private static string UniqueEmail() => $"anna.schmidt-{Guid.NewGuid():N}@example.com";

    /// <summary>
    /// The two carts — the account's holds one "Clean Architecture", added in another browser; a cookie-less guest holds
    /// two of it and the mug — and the guest logging in. Answers the guest's browser and the merge page it lands on.
    /// </summary>
    private async Task<(HttpClient Guest, string MergePage)> LogInWithTwoCartsAsync()
    {
        var email = UniqueEmail();
        var otherBrowser = NewBrowser();
        await RegisterAsync(otherBrowser, email);
        await AddOneUnitAsync(otherBrowser, CleanArchitecture);
        var guest = NewBrowser();
        await AddOneUnitAsync(guest, CleanArchitecture);
        await AddOneUnitAsync(guest, CleanArchitecture);
        await AddOneUnitAsync(guest, Mug);

        var page = await guest.GetStringAsync("/login");
        var loggedIn = await guest.PostAsync(
            "/login", Form(page, ("email", email), ("password", Password), ("returnUrl", string.Empty)));
        Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);
        var merge = await guest.GetAsync(loggedIn.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, merge.StatusCode);
        return (guest, await merge.Content.ReadAsStringAsync());
    }

    /// <summary>Sends the merge form with <paramref name="strategy"/>; answers where it leads.</summary>
    private static async Task<string> ChooseAsync(HttpClient browser, string mergePage, string strategy)
    {
        var anonymousUserId = Regex.Match(mergePage, @"name=""anonymousUserId"" value=""([^""]+)""").Groups[1].Value;
        var decided = await browser.PostAsync(
            "/cart/merge", Form(mergePage, ("strategy", strategy), ("anonymousUserId", anonymousUserId)));
        Assert.Equal(HttpStatusCode.Redirect, decided.StatusCode);
        return decided.Headers.Location!.ToString();
    }

    private static string Between(string page, string from, string to)
    {
        var start = page.IndexOf(from, StringComparison.Ordinal);
        var end = page.IndexOf(to, start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, $"the page has {from} before {to}");
        return page[start..end];
    }

    /// <summary>A merge-page cart box's title and its figures, in page order.</summary>
    private static IReadOnlyList<string> CartBoxFigures(string box) =>
        MergeBoxFigure.Matches(box).Select(m => WebUtility.HtmlDecode(m.Groups[1].Value).Trim()).ToList();

    /// <summary>A merge-page cart box's lines as <c>name, Qty: n, unit price, with image</c>, in page order.</summary>
    private static IReadOnlyList<string> CartBoxLines(string box) =>
        MergeBoxLine.Matches(box)
            .Select(m => string.Join(
                ", ",
                WebUtility.HtmlDecode(m.Groups[2].Value).Trim(),
                m.Groups[3].Value.Trim(),
                m.Groups[4].Value.Trim(),
                string.IsNullOrEmpty(m.Groups[1].Value) ? "without image" : "with image"))
            .ToList();

    /// <summary>The merge options as <c>title — description</c>, the checked one marked <c>(selected)</c>.</summary>
    private static IReadOnlyList<string> MergeOptionCards(string choice) =>
        MergeOptionCard.Matches(choice)
            .Select(m => $"{m.Groups[2].Value.Trim()} — {m.Groups[3].Value.Trim()}{(m.Groups[1].Success ? " (selected)" : string.Empty)}")
            .ToList();

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