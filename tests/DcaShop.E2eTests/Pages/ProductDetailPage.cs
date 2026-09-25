using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class ProductDetailPage : BasePage
{
    private const string UrlPattern = "/products/*";
    private const string ProductDetail = "product-detail";
    private const string AddToCartButton = "product-add-to-cart-button";
    private const string BackLink = "product-back-link";
    private const string Title = "product-detail-title";
    private const string PriceValue = "product-detail-price";

    private ProductDetailPage(IPage page) : base(page)
    {
    }

    public static async Task<ProductDetailPage> OpenAsync(IPage page)
    {
        var detail = new ProductDetailPage(page);
        await detail.WaitForUrlAsync(UrlPattern);
        await detail.WaitForAsync(ProductDetail);
        return detail;
    }

    public async Task<CartPage> AddToCartAsync()
    {
        await ClickAsync(AddToCartButton);
        await WaitForUrlAsync("/cart**");
        return await CartPage.OpenAsync(Page);
    }

    public async Task<ProductCatalogPage> BackToCatalogAsync()
    {
        await ClickAsync(BackLink);
        return await ProductCatalogPage.OpenAsync(Page);
    }

    public Task<bool> IsDisplayedAsync() => ExistsAsync(ProductDetail);

    public static async Task<ProductDetailPage> NavigateToAsync(IPage page, string path)
    {
        await page.GotoAsync(BaseUrl + path);
        return await OpenAsync(page);
    }

    public string ShownPath => CurrentPath;

    public async Task<string> TitleAsync() => (await Page.Locator($"[data-test='{Title}']").InnerTextAsync()).Trim();

    public async Task<string> PriceAsync() => (await Page.Locator($"[data-test='{PriceValue}']").InnerTextAsync()).Trim();

    /// <summary>The address of the product image the page shows.</summary>
    public Task<string> ImageSourceAsync() =>
        Page.Locator($"[data-test='{ProductDetail}'] img").First.EvaluateAsync<string>("e => e.currentSrc || e.getAttribute('src') || ''");
}