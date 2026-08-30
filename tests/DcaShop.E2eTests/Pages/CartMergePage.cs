using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>
/// The page a visitor lands on after logging in with items in both carts, where they decide which one to keep.
/// </summary>
public sealed class CartMergePage : BasePage
{
    private const string AnonymousCartSummary = "anonymous-cart-summary";
    private const string AccountCartSummary = "account-cart-summary";
    private const string MergeOptionMergeBoth = "merge-option-merge-both";
    private const string MergeOptionUseAccount = "merge-option-use-account";
    private const string MergeOptionUseAnonymous = "merge-option-use-anonymous";
    private const string MergeSubmitButton = "merge-submit-button";

    public CartMergePage(IPage page) : base(page)
    {
    }

    public bool IsOnMergePage => CurrentPath.StartsWith("/cart/merge", StringComparison.Ordinal);

    public Task<bool> ShowsAnonymousCartAsync() => ExistsAsync(AnonymousCartSummary);

    public Task<bool> ShowsAccountCartAsync() => ExistsAsync(AccountCartSummary);

    public async Task<bool> ShowsMergeOptionsAsync() =>
        await ExistsAsync(MergeOptionMergeBoth)
        && await ExistsAsync(MergeOptionUseAccount)
        && await ExistsAsync(MergeOptionUseAnonymous);

    public Task<CartPage> MergeBothCartsAsync() => SubmitAsync(MergeOptionMergeBoth);

    public Task<CartPage> UseAccountCartOnlyAsync() => SubmitAsync(MergeOptionUseAccount);

    public Task<CartPage> UseAnonymousCartOnlyAsync() => SubmitAsync(MergeOptionUseAnonymous);

    private async Task<CartPage> SubmitAsync(string option)
    {
        await Page.Locator($"[data-test='{option}'] input[type='radio']").CheckAsync();
        await ClickAsync(MergeSubmitButton);
        return await CartPage.OpenAsync(Page);
    }
}
