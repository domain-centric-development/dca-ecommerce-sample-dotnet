using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class ProductDetailPage : BasePage
{
    private const string UrlPattern = "/products/*";
    private const string ProductDetail = "product-detail";
    private const string AddToCartButton = "product-add-to-cart-button";
    private const string BackLink = "product-back-link";

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
}
