using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// Guest checkout through the browser — the same four scenarios, page objects and <c>data-test</c> selectors as
/// the Java sample's <c>CheckoutGuestE2ETest</c>; either suite can be pointed at either shop.
/// </summary>
public sealed class CheckoutGuestE2eTest : BaseE2eTest
{
    public CheckoutGuestE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Complete checkout flow as guest user")]
    public async Task CompleteGuestCheckoutFlow()
    {
        // Step 1: Navigate to product catalog and add product to cart
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var detail = await catalog.ViewFirstProductAsync();
        await detail.AddToCartAsync();

        // Step 2: Navigate to cart and verify product is added
        var cart = await CartPage.NavigateToAsync(Page);
        Assert.True(await cart.HasItemsAsync(), "Cart should have at least one item");

        // Step 3: Start checkout and fill buyer information
        var buyer = await cart.ProceedToCheckoutAsync();
        var delivery = await (await buyer.FillBuyerInfoAsync("guest@example.com", "Test", "Guest", "+1-555-0100")).ContinueToDeliveryAsync();

        // Step 4: Fill delivery information
        await delivery.FillAddressAsync("123 Main Street", "Springfield", "12345", "United States", "IL");
        await delivery.SelectFirstShippingOptionAsync();
        var payment = await delivery.ContinueToPaymentAsync();

        // Step 5: Select payment method
        await payment.SelectFirstPaymentProviderAsync();
        var review = await payment.ContinueToReviewAsync();

        // Step 6: Verify order details and place order
        Assert.True(await review.ShowsEmailAsync("guest@example.com"), "Review page should show buyer email");
        Assert.True(await review.ShowsAddressAsync("123 Main Street"), "Review page should show delivery address");
        var confirmation = await review.PlaceOrderAsync();

        // Step 7: Verify confirmation
        Assert.True(await confirmation.IsOrderConfirmedAsync(), "Confirmation page should show success message");
    }

    [E2eFact(DisplayName = "Checkout should redirect to cart if no active session")]
    public async Task CheckoutWithNoActiveSessionRedirectsToCart()
    {
        await NavigateToAsync("/checkout/buyer");

        await WaitForUrlAsync("/cart**");
        Assert.StartsWith("/cart", CurrentPath, StringComparison.Ordinal);
    }

    [E2eFact(DisplayName = "Buyer info validation shows errors for invalid input")]
    public async Task BuyerInfoValidationShowsErrors()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var detail = await catalog.ViewFirstProductAsync();
        await detail.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();

        await (await buyer.FillBuyerInfoAsync("invalid-email", "Test", "User", string.Empty)).SubmitWithErrorsAsync();

        Assert.True(await buyer.HasValidationErrorsAsync(), "Should show email validation error");
        Assert.True(buyer.IsOnPage, "Should stay on buyer info page on validation error");
    }

    [E2eFact(DisplayName = "Can navigate back through checkout steps")]
    public async Task CanNavigateBackThroughCheckoutSteps()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var detail = await catalog.ViewFirstProductAsync();
        await detail.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        var delivery = await (await buyer.FillBuyerInfoAsync("guest@example.com", "Test", "Guest", "+1-555-0100")).ContinueToDeliveryAsync();

        if (await delivery.HasBackLinkAsync())
        {
            var backToBuyer = await delivery.GoBackAsync();
            Assert.True(backToBuyer.IsOnPage, "Should navigate back to buyer info");
        }
    }
}
