using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest tells the checkout who they are and how to reach them.</summary>
public sealed class EnterBuyerDetailsE2eTest : BaseE2eTest
{
    public EnterBuyerDetailsE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Valid details open the delivery step")]
    public async Task SubmittingValidBuyerDetailsOpensTheDeliveryStep()
    {
        var buyer = await GuestOnTheBuyerStepAsync();

        var shownPath = await (await buyer.FillBuyerInfoAsync("jane@example.com", "Jane", "Doe", "+49 30 1234567")).SubmitAndShowNextPathAsync();

        Assert.Equal("/checkout/delivery", shownPath);
    }

    [E2eFact(DisplayName = "Invalid email and missing phone keep the buyer step")]
    public async Task AnInvalidEmailAndNoPhoneKeepTheGuestOnTheBuyerStep()
    {
        var buyer = await GuestOnTheBuyerStepAsync();
        await buyer.FillBuyerInfoAsync("not-an-email", "Jane", "Doe", string.Empty);
        var visited = new List<string>();
        Page.Request += (_, request) =>
        {
            if (request.IsNavigationRequest)
            {
                visited.Add(new Uri(request.Url).AbsolutePath);
            }
        };

        await buyer.PressContinueToDeliveryAsync();

        Assert.True(buyer.IsOnPage, "the buyer step stays open");
        Assert.Equal(new[] { "email", "phone" }, await buyer.FieldsTheBrowserRefusesAsync());
        Assert.DoesNotContain("/checkout/delivery", visited);
    }

    private async Task<BuyerInfoPage> GuestOnTheBuyerStepAsync()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");
        await product.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        return await cart.ProceedToCheckoutAsync();
    }
}