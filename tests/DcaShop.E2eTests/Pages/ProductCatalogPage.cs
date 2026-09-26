using System.Text.RegularExpressions;

using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class ProductCatalogPage : BasePage
{
    private const string UrlPattern = "/products";
    private const string ProductCard = "product-card";
    private const string ViewDetailsLink = "view-product";
    private const string ProductTitle = "product-card-title";
    private const string Breadcrumb = "breadcrumb";

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

    /// <summary>Follows "View Details" on the card whose title is <paramref name="name"/>.</summary>
    public async Task<ProductDetailPage> ViewProductAsync(string name)
    {
        await Page.Locator($"[data-test='{ProductCard}']")
            .Filter(new LocatorFilterOptions { Has = Page.Locator($"[data-test='{ProductTitle}']", new PageLocatorOptions { HasTextRegex = new Regex($"^\\s*{Regex.Escape(name)}\\s*$") }) })
            .Locator($"[data-test='{ViewDetailsLink}']")
            .ClickAsync();
        return await ProductDetailPage.OpenAsync(Page);
    }

    public Task<bool> HasProductsAsync() => ExistsAsync(ProductCard);

    /// <summary>The document title, as the browser tab shows it.</summary>
    public Task<string> DocumentTitleAsync() => Page.TitleAsync();

    /// <summary>The breadcrumb trail as one line, segments separated by single spaces ("Home / Products").</summary>
    public async Task<string> BreadcrumbAsync() =>
        string.Join(' ', ((await Page.Locator($"[data-test='{Breadcrumb}']").TextContentAsync()) ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>The product names as the catalog shows them, in page order.</summary>
    public async Task<IReadOnlyList<string>> ProductTitlesAsync() =>
        (await Page.Locator($"[data-test='{ProductTitle}']").AllTextContentsAsync()).Select(t => t.Trim()).ToList();
}