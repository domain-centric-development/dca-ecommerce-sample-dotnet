using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class CartPage : BasePage
{
    private const string UrlPattern = "/cart**";
    private const string CartItem = "cart-item";
    private const string CheckoutLink = "cart-checkout-link";

    private CartPage(IPage page) : base(page)
    {
    }

    public static async Task<CartPage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + "/cart");
        return await OpenAsync(page);
    }

    public static async Task<CartPage> OpenAsync(IPage page)
    {
        var cart = new CartPage(page);
        await cart.WaitForUrlAsync(UrlPattern);
        return cart;
    }

    public async Task<BuyerInfoPage> ProceedToCheckoutAsync()
    {
        await WaitForAsync(CartItem);
        await ClickAsync(CheckoutLink);
        return await BuyerInfoPage.OpenAsync(Page);
    }

    public Task<bool> HasItemsAsync() => ExistsAsync(CartItem);

    public Task<int> ItemCountAsync() => Page.Locator($"[data-test='{CartItem}']").CountAsync();
}
