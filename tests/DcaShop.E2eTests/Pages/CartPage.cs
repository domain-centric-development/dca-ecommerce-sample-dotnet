using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class CartPage : BasePage
{
    private const string UrlPattern = "/cart**";
    private const string CartItem = "cart-item";
    private const string CheckoutButton = "cart-checkout-button";
    private const string SuccessMessage = "cart-success-message";
    private const string ErrorMessage = "cart-error-message";
    private const string RemoveButton = "cart-item-remove";

    private CartPage(IPage page) : base(page)
    {
    }

    public static async Task<CartPage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + "/cart");
        return await OpenAsync(page);
    }

    /// <summary>Opens the cart page of the shop at <paramref name="shopUrl"/>, a shop other than the suite's own, with its lines.</summary>
    public static async Task<CartPage> NavigateToAsync(IPage page, string shopUrl)
    {
        await page.GotoAsync(shopUrl + "/cart");
        var cart = new CartPage(page);
        await cart.WaitForAsync(CartItem);
        return cart;
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

    /// <summary>
    /// Presses "Proceed to Checkout" and waits until the browser has left the cart, wherever the shop sends it. The page
    /// it lands on is <see cref="ShownPath"/>.
    /// </summary>
    public async Task PressProceedToCheckoutAsync()
    {
        var button = Page.Locator($"[data-test='{CheckoutButton}']");
        await Assertions.Expect(button).ToHaveTextAsync("Proceed to Checkout");
        await button.ClickAsync();
        await Page.WaitForLoadedUrlAsync(url => new Uri(url).AbsolutePath != "/cart");
    }

    public string ShownPath => CurrentPath;

    /// <summary>The success message the page shows after a product was added.</summary>
    public async Task<string> SuccessMessageAsync() => (await Page.Locator($"[data-test='{SuccessMessage}']").InnerTextAsync()).Trim();

    /// <summary>The error message the page shows, waited for until it is visible.</summary>
    public Task<string> ErrorMessageAsync() => VisibleTextAsync(ErrorMessage);

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

    /// <summary>What the line of the named product shows: its image's address, quantity, unit price and line total.</summary>
    public async Task<CartLine> LineAsync(string productName)
    {
        var item = Page.Locator($"[data-test='{CartItem}']")
            .Filter(new LocatorFilterOptions { Has = Page.Locator("[data-test='cart-item-name']", new PageLocatorOptions { HasTextString = productName }) });
        await Assertions.Expect(item).ToHaveCountAsync(1);
        return new CartLine(
            await item.Locator(".cart__item-image img").GetAttributeAsync("src") ?? string.Empty,
            (await item.Locator("[data-test='cart-item-quantity']").InnerTextAsync()).Trim(),
            await ValueBesideAsync(item, "Unit Price"),
            await ValueBesideAsync(item, "Line Total"));
    }

    /// <summary>The stock warnings the line of the named product shows, as written; empty when it shows none.</summary>
    public async Task<IReadOnlyList<string>> StockWarningsAsync(string productName)
    {
        var item = Page.Locator($"[data-test='{CartItem}']")
            .Filter(new LocatorFilterOptions { Has = Page.Locator("[data-test='cart-item-name']", new PageLocatorOptions { HasTextString = productName }) });
        await Assertions.Expect(item).ToHaveCountAsync(1);
        return (await item.Locator("[data-test='stock-alert']").AllInnerTextsAsync()).Select(t => t.Trim()).ToList();
    }

    /// <summary>
    /// Presses "Remove" on the line of the named product and waits until the cart page the shop answers with has loaded
    /// — the same address, a new document.
    /// </summary>
    public async Task RemoveAsync(string productName)
    {
        var item = Page.Locator($"[data-test='{CartItem}']")
            .Filter(new LocatorFilterOptions { Has = Page.Locator("[data-test='cart-item-name']", new PageLocatorOptions { HasTextString = productName }) });
        await Assertions.Expect(item).ToHaveCountAsync(1);
        var remove = item.Locator($"[data-test='{RemoveButton}']");
        await Assertions.Expect(remove).ToHaveTextAsync("Remove");
        await Page.EvaluateAsync("() => { window.__beforeRemove = true; }");
        await remove.ClickAsync();
        await Page.WaitForFunctionAsync("() => window.__beforeRemove !== true && document.readyState === 'complete'");
    }

    /// <summary>The value shown beside a label in a line. The markup carries no data-test for it.</summary>
    private async Task<string> ValueBesideAsync(ILocator item, string label) =>
        (await item.Locator(".cart__item-row")
            .Filter(new LocatorFilterOptions { Has = Page.Locator(".cart__item-label", new PageLocatorOptions { HasTextString = label }) })
            .Locator("span")
            .Nth(1)
            .InnerTextAsync()).Trim();

    public sealed record CartLine(string ImageSource, string Quantity, string UnitPrice, string LineTotal);
}