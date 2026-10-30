using System.Net;
using System.Text.RegularExpressions;

using DcaShop.Account.Application.AuthenticateAccount;
using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetActiveCart;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Logging in to an existing account through the real HTTP pipeline: the account's cart is shown when the guest
/// collected nothing, a guest's open checkout follows the cart into the account, and the customer checks out like a guest.
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

        var landing = await LogInAsync(browser, email);

        Assert.Equal("/cart", landing);
        Assert.Equal([$"{CleanArchitecture} x1"], CartLines(await browser.GetStringAsync("/cart")));
    }

    [Fact]
    public async Task LoggingInFromTheCheckoutReturnsToTheBuyerStepWithTheGuestsOrder()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewBrowser(), email);
        var guest = NewBrowser();
        await AddOneUnitAsync(guest, DomainDrivenDesign);
        Assert.Equal("/checkout/buyer", await StartCheckoutAsync(guest));

        var landing = await LogInTakingTheCartOverAsync(guest, email, loginPage: "/login?returnUrl=/checkout/buyer");
        var buyerStep = await guest.GetAsync(landing);

        Assert.Equal("/checkout/buyer", landing);
        Assert.Equal(HttpStatusCode.OK, buyerStep.StatusCode);
        var summary = AvailabilityPages.Section(await buyerStep.Content.ReadAsStringAsync(), "order-summary-item");
        Assert.Equal([$"{DomainDrivenDesign} x1"], SummaryItems(summary));
    }

    [Fact]
    public async Task OpeningThePageAfterTheLoginLeavesTheAccountsCartEmpty()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewBrowser(), email);
        var guest = NewBrowser();
        await AddOneUnitAsync(guest, Mug);
        var page = await guest.GetStringAsync("/login");
        var loggedIn = await guest.PostAsync(
            "/login", Form(page, ("email", email), ("password", Password), ("returnUrl", string.Empty)));

        await guest.GetAsync(loggedIn.Headers.Location);

        Assert.Empty(await AccountCartLinesAsync(email));
    }

    [Fact]
    public async Task ALoggedInCustomerStartsTheCheckoutAtTheBuyerStep()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewBrowser(), email);
        var browser = NewBrowser();
        await LogInAsync(browser, email);
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

    [Fact]
    public async Task MergingAnotherBrowsersGuestCartLeavesBothCartsAsTheyWere()
    {
        var email = UniqueEmail();
        var otherBrowser = NewBrowser();
        await RegisterAsync(otherBrowser, email);
        await AddOneUnitAsync(otherBrowser, CleanArchitecture);
        var annasBrowser = NewBrowser();
        Assert.Equal("/cart", await LogInAsync(annasBrowser, email));
        var annasCart = await annasBrowser.GetStringAsync("/cart");
        var otherGuest = await AnotherGuestWithOneUnitAsync(annasBrowser, Mug);

        var decided = await annasBrowser.PostAsync(
            "/cart/merge",
            Forms.Submission(annasCart, ("strategy", "MERGE_BOTH"), ("anonymousUserId", otherGuest)));

        Assert.Equal(HttpStatusCode.Redirect, decided.StatusCode);
        Assert.Equal("/cart", decided.Headers.Location!.ToString());
        var cartPage = await annasBrowser.GetStringAsync("/cart");
        Assert.Equal([$"{CleanArchitecture} x1"], CartLines(cartPage));
        Assert.DoesNotContain(@"data-test=""cart-success-message""", cartPage, StringComparison.Ordinal);
        Assert.Equal([$"{Mug} x1"], await GuestCartLinesAsync(otherGuest));
    }

    [Fact]
    public async Task MergingAfterALoginWithAnOutsideReturnPageOpensTheCart()
    {
        var (guest, mergePage) = await LogInWithTwoCartsAsync(loginPage: "/login?returnUrl=https://elsewhere.example/");

        var mergeForm = AvailabilityPages.Section(mergePage, "merge-form");
        var decided = await guest.PostAsync(
            "/cart/merge", Forms.AsRendered(mergeForm[..mergeForm.IndexOf("</form>", StringComparison.Ordinal)], ("strategy", "MERGE_BOTH")));

        Assert.Equal(HttpStatusCode.Redirect, decided.StatusCode);
        Assert.Equal("/cart", decided.Headers.Location!.ToString());
        var cartPage = await guest.GetStringAsync("/cart");
        Assert.Equal("Carts merged successfully!", AvailabilityPages.Text(cartPage, @"data-test=""cart-success-message"">([^<]*)</div>"));
    }

    [Fact]
    public async Task TakingTheCartOverAfterALoginWithAnOutsideReturnPageOpensTheCart()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewBrowser(), email);
        var guest = NewBrowser();
        await AddOneUnitAsync(guest, CleanArchitecture);
        var takeOverPage = await LogInToTheTakeOverPageAsync(guest, email, loginPage: "/login?returnUrl=https://elsewhere.example/");

        // The take-over carries the return page the login was opened for — the login's own form drops it already, so
        // sent as rendered it would never reach the take-over.
        var takeOverForm = AvailabilityPages.Section(takeOverPage, "take-over-form");
        var takenOver = await guest.PostAsync(
            "/cart/merge/take-over",
            Forms.AsRendered(
                takeOverForm[..takeOverForm.IndexOf("</form>", StringComparison.Ordinal)],
                ("returnUrl", "https://elsewhere.example/")));

        Assert.Equal(HttpStatusCode.Redirect, takenOver.StatusCode);
        Assert.Equal("/cart", takenOver.Headers.Location!.ToString());
        Assert.Equal([$"{CleanArchitecture} x1"], CartLines(await guest.GetStringAsync("/cart")));
    }

    private static string UniqueEmail() => $"anna.schmidt-{Guid.NewGuid():N}@example.com";

    /// <summary>
    /// A guest in another browser who added one unit of <paramref name="productName"/> to the cart, arranged through the
    /// Cart's own ports; answers the guest's identity. <paramref name="anyBrowser"/> only finds the product's id.
    /// </summary>
    private async Task<string> AnotherGuestWithOneUnitAsync(HttpClient anyBrowser, string productName)
    {
        var guest = UserId.GenerateAnonymous().Value;
        var productPath = await AvailabilityPages.ProductPathAsync(anyBrowser, productName);
        using var scope = _factory.Services.CreateScope();
        var cart = await scope.ServiceProvider.GetRequiredService<IGetOrCreateActiveCartInputPort>()
            .ExecuteAsync(new GetOrCreateActiveCartCommand(guest));
        await scope.ServiceProvider.GetRequiredService<IAddItemToCartInputPort>()
            .ExecuteAsync(new AddItemToCartCommand(cart.CartId, guest, Guid.Parse(productPath["/products/".Length..]), 1));
        return guest;
    }

    /// <summary>The lines of <paramref name="guest"/>'s cart as <c>name xquantity</c>, read through the Cart's own query.</summary>
    private async Task<IReadOnlyList<string>> GuestCartLinesAsync(string guest)
    {
        using var scope = _factory.Services.CreateScope();
        var cart = await scope.ServiceProvider.GetRequiredService<IGetActiveCartInputPort>()
            .ExecuteAsync(new GetActiveCartQuery(guest));
        return (cart.Cart?.Items ?? []).Select(item => $"{item.Article.Name} x{item.Quantity.Value}").ToList();
    }

    /// <summary>
    /// The two carts — the account's holds one "Clean Architecture", added in another browser; a cookie-less guest holds
    /// two of it and the mug — and the guest logging in from <paramref name="loginPage"/> with the return page it
    /// carries. Answers the guest's browser and the merge page it lands on.
    /// </summary>
    private async Task<(HttpClient Guest, string MergePage)> LogInWithTwoCartsAsync(string loginPage = "/login")
    {
        var email = UniqueEmail();
        var otherBrowser = NewBrowser();
        await RegisterAsync(otherBrowser, email);
        await AddOneUnitAsync(otherBrowser, CleanArchitecture);
        var guest = NewBrowser();
        await AddOneUnitAsync(guest, CleanArchitecture);
        await AddOneUnitAsync(guest, CleanArchitecture);
        await AddOneUnitAsync(guest, Mug);

        var page = await guest.GetStringAsync(loginPage);
        var returnUrl = WebUtility.HtmlDecode(
            Regex.Match(page, @"<input type=""hidden"" name=""returnUrl"" value=""([^""]*)"">").Groups[1].Value);
        var loggedIn = await guest.PostAsync(
            "/login", Form(page, ("email", email), ("password", Password), ("returnUrl", returnUrl)));
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
    /// <summary>
    /// Logs in from <paramref name="loginPage"/> with the form as that page renders it — its token and the return
    /// address it carries — and follows the merge decision to where the login lands.
    /// </summary>
    private static async Task<string> LogInAsync(HttpClient browser, string email, string loginPage = "/login")
    {
        var page = await browser.GetStringAsync(loginPage);
        var returnUrl = WebUtility.HtmlDecode(
            Regex.Match(page, @"<input type=""hidden"" name=""returnUrl"" value=""([^""]*)"">").Groups[1].Value);
        var loggedIn = await browser.PostAsync(
            "/login", Form(page, ("email", email), ("password", Password), ("returnUrl", returnUrl)));
        Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);
        Assert.StartsWith("/cart/merge", loggedIn.Headers.Location!.ToString(), StringComparison.Ordinal);

        var merged = await browser.GetAsync(loggedIn.Headers.Location);
        Assert.Equal(HttpStatusCode.Redirect, merged.StatusCode);
        return merged.Headers.Location!.ToString();
    }

    /// <summary>
    /// Logs in from <paramref name="loginPage"/> with a guest cart the account takes over: the page after the login
    /// opens, its form is sent, and the answer is where the login lands.
    /// </summary>
    private static async Task<string> LogInTakingTheCartOverAsync(HttpClient browser, string email, string loginPage)
    {
        var page = await browser.GetStringAsync(loginPage);
        var returnUrl = WebUtility.HtmlDecode(
            Regex.Match(page, @"<input type=""hidden"" name=""returnUrl"" value=""([^""]*)"">").Groups[1].Value);
        var loggedIn = await browser.PostAsync(
            "/login", Form(page, ("email", email), ("password", Password), ("returnUrl", returnUrl)));
        Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);

        var takeOverPage = await browser.GetAsync(loggedIn.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, takeOverPage.StatusCode);
        var takenOver = await TakeOverForm.SendAsync(browser, await takeOverPage.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Redirect, takenOver.StatusCode);
        return takenOver.Headers.Location!.ToString();
    }

    /// <summary>
    /// Logs in from <paramref name="loginPage"/> with a guest cart the account takes over, and answers the page after
    /// the login as rendered — the take-over not yet sent.
    /// </summary>
    private static async Task<string> LogInToTheTakeOverPageAsync(HttpClient browser, string email, string loginPage)
    {
        var page = await browser.GetStringAsync(loginPage);
        var returnUrl = WebUtility.HtmlDecode(
            Regex.Match(page, @"<input type=""hidden"" name=""returnUrl"" value=""([^""]*)""").Groups[1].Value);
        var loggedIn = await browser.PostAsync(
            "/login", Form(page, ("email", email), ("password", Password), ("returnUrl", returnUrl)));
        Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);

        var takeOverPage = await browser.GetAsync(loggedIn.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, takeOverPage.StatusCode);
        return await takeOverPage.Content.ReadAsStringAsync();
    }

    /// <summary>The lines of the cart of the account <paramref name="email"/> holds, read through the Cart's own query.</summary>
    private async Task<IReadOnlyList<EnrichedCartItem>> AccountCartLinesAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var account = await scope.ServiceProvider.GetRequiredService<IAuthenticateAccountInputPort>()
            .ExecuteAsync(new AuthenticateAccountCommand(email, Password));
        var cart = await scope.ServiceProvider.GetRequiredService<IGetActiveCartInputPort>()
            .ExecuteAsync(new GetActiveCartQuery(account.UserId!));
        return cart.Cart?.Items ?? [];
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

    /// <summary>A checkout step's order summary as <c>name xquantity</c>, in page order.</summary>
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