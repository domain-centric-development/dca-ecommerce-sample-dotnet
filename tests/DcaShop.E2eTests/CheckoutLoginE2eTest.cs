using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// Checkout as an authenticated visitor — the same four scenarios, page objects and <c>data-test</c> selectors
/// as the Java sample's <c>CheckoutLoginE2ETest</c>; either suite can be pointed at either shop.
/// </summary>
public sealed class CheckoutLoginE2eTest : BaseE2eTest
{
    private const string TestPassword = "SecurePassword123!";

    public CheckoutLoginE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Complete checkout flow as authenticated user")]
    public async Task CompleteAuthenticatedCheckoutFlow()
    {
        var email = UniqueEmail("testuser");
        await RegisterAsync(email);
        await AddProductToCartAsync();

        var cart = await CartPage.NavigateToAsync(Page);
        Assert.True(await cart.HasItemsAsync(), "Cart should have items");

        var buyer = await cart.ProceedToCheckoutAsync();
        var delivery = await (await buyer.FillBuyerInfoAsync(email, "Test", "User", "+1-555-0199"))
            .ContinueToDeliveryAsync();

        await delivery.FillAddressAsync("456 Oak Avenue", "Chicago", "60601", "United States", "IL");
        await delivery.SelectFirstShippingOptionAsync();
        var payment = await delivery.ContinueToPaymentAsync();

        await payment.SelectFirstPaymentProviderAsync();
        var review = await payment.ContinueToReviewAsync();
        Assert.True(await review.ShowsEmailAsync(email), "Review page should show user email");

        var confirmation = await review.PlaceOrderAsync();
        Assert.True(await confirmation.IsOrderConfirmedAsync(), "Confirmation page should show success message");
    }

    [E2eFact(DisplayName = "Registered user can add items to cart")]
    public async Task RegisteredUserCanAddToCart()
    {
        await RegisterAsync(UniqueEmail("cartuser"));
        await AddProductToCartAsync();

        var cart = await CartPage.NavigateToAsync(Page);
        Assert.True(await cart.ItemCountAsync() > 0, "Registered user cart should have items");
    }

    [E2eFact(DisplayName = "Can login and checkout with existing account")]
    public async Task CanLoginAndCheckoutWithExistingAccount()
    {
        var email = UniqueEmail("existing");
        await RegisterAsync(email);

        // A different browser session entirely — the account is all that carries over.
        await ClearCookiesAsync();
        var login = await LoginPage.NavigateToAsync(Page);
        await login.LoginAsync(email, TestPassword);

        await AddProductToCartAsync();

        var cart = await CartPage.NavigateToAsync(Page);
        Assert.True(await cart.HasItemsAsync(), "Cart should have items");
        var buyer = await cart.ProceedToCheckoutAsync();
        Assert.True(buyer.IsOnPage, "Authenticated user should proceed to checkout");
    }

    [E2eFact(DisplayName = "Checkout link from login page redirects to checkout after login")]
    public async Task LoginFromCheckoutRedirectsBackToCheckout()
    {
        await AddProductToCartAsync();

        var cart = await CartPage.NavigateToAsync(Page);
        Assert.True(await cart.HasItemsAsync(), "Cart should have items");
        await cart.ProceedToCheckoutAsync();

        if (CurrentPath.Contains("/login", StringComparison.Ordinal))
        {
            var login = await LoginPage.OpenAsync(Page);
            var register = await login.GoToRegisterAsync();
            await register.RegisterAsync(UniqueEmail("redirect"), TestPassword);
        }

        await Page.WaitForURLAsync(url =>
            url.Contains("/checkout", StringComparison.Ordinal) || url.Contains("/cart", StringComparison.Ordinal));

        Assert.True(
            CurrentPath.Contains("/checkout", StringComparison.Ordinal)
            || CurrentPath.Contains("/cart", StringComparison.Ordinal),
            $"Should be redirected to checkout or cart after login. Got: {CurrentPath}");
    }

    [E2eFact(DisplayName = "Buyer step drops the login prompt and prefills the email once logged in")]
    public async Task BuyerStepReflectsLoginState()
    {
        // As a guest the buyer step offers login and register and leaves the email blank.
        await AddProductToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        Assert.True(await cart.HasItemsAsync(), "Cart should have items");
        var guestBuyer = await cart.ProceedToCheckoutAsync();

        Assert.True(await guestBuyer.ShowsAuthOptionsAsync(), "A guest should be offered login and register");
        Assert.Equal(string.Empty, await guestBuyer.EmailValueAsync());

        // Registering from within the checkout turns the same visitor into a registered one.
        var email = UniqueEmail("buyerstate");
        await RegisterAsync(email);

        var cartAfterLogin = await CartPage.NavigateToAsync(Page);
        Assert.True(await cartAfterLogin.HasItemsAsync(), "Cart should have items");
        var buyer = await cartAfterLogin.ProceedToCheckoutAsync();

        Assert.False(await buyer.ShowsAuthOptionsAsync(), "A logged-in visitor must not be asked to log in again");
        Assert.Contains(email, await buyer.LoggedInBannerTextAsync(), StringComparison.Ordinal);
        Assert.Equal(email, await buyer.EmailValueAsync());
    }

    private static string UniqueEmail(string prefix) =>
        $"{prefix}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid():N}@example.com";

    private async Task RegisterAsync(string email)
    {
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync(email, TestPassword);
    }

    private async Task AddProductToCartAsync()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var detail = await catalog.ViewFirstProductAsync();
        await detail.AddToCartAsync();
    }
}
