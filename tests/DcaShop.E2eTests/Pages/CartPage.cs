using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class CartPage : BasePage
{
    private const string UrlPattern = "/cart**";
    private const string CartItem = "cart-item";
    private const string CheckoutButton = "cart-checkout-button";

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
        await ClickAsync(CheckoutButton);
        return await BuyerInfoPage.OpenAsync(Page);
    }

    public Task<bool> HasItemsAsync() => ExistsAsync(CartItem);

    public Task<int> ItemCountAsync() => Page.Locator($"[data-test='{CartItem}']").CountAsync();

    /// <summary>The cart lines as <c>name x quantity</c> pairs, in display order.</summary>
    public async Task<IReadOnlyList<string>> LineItemsAsync()
    {
        var items = Page.Locator($"[data-test='{CartItem}']");
        var lines = new List<string>();
        for (var i = 0; i < await items.CountAsync(); i++)
        {
            var item = items.Nth(i);
            var name = (await item.Locator("[data-test='cart-item-name']").TextContentAsync() ?? string.Empty).Trim();
            var quantity = (await item.Locator("[data-test='cart-item-quantity']").TextContentAsync() ?? string.Empty).Trim();
            lines.Add($"{name} x{quantity}");
        }
        return lines;
    }
}
