using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>
/// The page a guest lands on after logging in with items in the cart and none in the account's: it takes the cart over
/// with the form it sends — by itself where the browser runs scripts, with "Continue" where it does not.
/// </summary>
public sealed class CartTakeOverPage : BasePage
{
    private const string Message = "take-over-message";
    private const string ContinueButton = "take-over-continue-button";

    private CartTakeOverPage(IPage page) : base(page)
    {
    }

    /// <summary>The page as the login left it; whether it is shown is what <see cref="MessageAsync"/> waits for.</summary>
    public static CartTakeOverPage Of(IPage page) => new(page);

    /// <summary>The sentence the page says, waited for until it is visible.</summary>
    public Task<string> MessageAsync() => VisibleTextAsync(Message);

    /// <summary>Presses "Continue" and follows the take-over to the page it leads to.</summary>
    public async Task<CartPage> ContinueAsync()
    {
        await ClickAsync(ContinueButton);
        await Page.WaitForLoadedUrlAsync(url => !url.Contains("/cart/merge", StringComparison.Ordinal));
        return await CartPage.OpenAsync(Page);
    }
}