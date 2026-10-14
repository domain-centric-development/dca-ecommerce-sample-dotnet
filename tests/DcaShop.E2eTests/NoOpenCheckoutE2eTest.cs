using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A visitor without an open checkout who opens a step page is sent to the cart with the reason.</summary>
public sealed class NoOpenCheckoutE2eTest : BaseE2eTest
{
    public NoOpenCheckoutE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Buyer step without a checkout leads to the cart")]
    public async Task TheBuyerStepWithoutACheckoutOpensTheCartWithTheReason()
    {
        await NavigateToAsync("/checkout/buyer");

        var cart = await CartPage.OpenAsync(Page);
        Assert.Equal("/cart?notice=no-active-checkout", cart.ShownPath);
        Assert.Equal("No active checkout session found", await cart.ErrorMessageAsync());
    }
}