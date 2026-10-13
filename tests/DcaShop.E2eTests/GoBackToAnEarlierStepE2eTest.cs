using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest goes back from the delivery step and finds the buyer step as it was submitted.</summary>
public sealed class GoBackToAnEarlierStepE2eTest : BaseE2eTest
{
    public GoBackToAnEarlierStepE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Back from delivery opens the buyer step")]
    public async Task TheDeliveryStepsBackLinkOpensTheBuyerStepWithTheSubmittedDetails()
    {
        var delivery = await GuestOnTheDeliveryStepAsync();

        var buyer = await delivery.GoBackAsync();

        Assert.Equal("/checkout/buyer", buyer.ShownPath);
        Assert.Equal(new[] { "jane@example.com", "Jane", "Doe", "+49 30 1234567" }, await buyer.FieldValuesAsync());
    }

    private async Task<DeliveryPage> GuestOnTheDeliveryStepAsync()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");
        await product.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        return await (await buyer.FillBuyerInfoAsync("jane@example.com", "Jane", "Doe", "+49 30 1234567")).ContinueToDeliveryAsync();
    }
}