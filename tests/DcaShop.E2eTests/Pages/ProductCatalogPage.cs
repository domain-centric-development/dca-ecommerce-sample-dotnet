using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class ProductCatalogPage : BasePage
{
    private const string UrlPattern = "/products";
    private const string ProductCard = "product-card";
    private const string ViewDetailsLink = "view-product";

    private ProductCatalogPage(IPage page) : base(page)
    {
    }

    public static async Task<ProductCatalogPage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + UrlPattern);
        return await OpenAsync(page);
    }

    public static async Task<ProductCatalogPage> OpenAsync(IPage page)
    {
        var catalog = new ProductCatalogPage(page);
        await catalog.WaitForUrlAsync(UrlPattern);
        await catalog.WaitForAsync(ProductCard);
        return catalog;
    }

    public async Task<ProductDetailPage> ViewFirstProductAsync()
    {
        await ClickFirstAsync(ViewDetailsLink);
        return await ProductDetailPage.OpenAsync(Page);
    }

    public async Task<ProductDetailPage> ViewProductAsync(int index)
    {
        await Page.Locator($"[data-test='{ViewDetailsLink}']").Nth(index).ClickAsync();
        return await ProductDetailPage.OpenAsync(Page);
    }

    public Task<bool> HasProductsAsync() => ExistsAsync(ProductCard);
}
