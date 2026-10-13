using System.Text.RegularExpressions;

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

    /// <summary>Opens the product page at <paramref name="path"/> of the shop at <paramref name="shopUrl"/>, a shop other than the suite's own.</summary>
    public static async Task<ProductDetailPage> NavigateToAsync(IPage page, string shopUrl, string path)
    {
        await page.GotoAsync(shopUrl + path);
        var detail = new ProductDetailPage(page);
        await detail.WaitForAsync(ProductDetail);
        return detail;
    }

    /// <summary>Presses "Add to Cart" and waits until the shop shows its cart page, whichever shop it is.</summary>
    public async Task PressAddToCartAsync()
    {
        var button = Page.Locator($"[data-test='{AddToCartButton}']");
        await Assertions.Expect(button).ToBeVisibleAsync();
        await button.ClickAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex("^https?://[^/]+/cart$"));
    }

    public string ShownPath => CurrentPath;

    public async Task<string> TitleAsync() => (await Page.Locator($"[data-test='{Title}']").InnerTextAsync()).Trim();

    public async Task<string> PriceAsync() => (await Page.Locator($"[data-test='{PriceValue}']").InnerTextAsync()).Trim();

    /// <summary>
    /// The stock badge in the page's header ("In Stock", "Out of Stock"), as written — the style shows it in capitals.
    /// The markup carries no data-test for it.
    /// </summary>
    public async Task<string> StockBadgeAsync() =>
        ((await Page.Locator($"[data-test='{ProductDetail}'] .product-detail__header .badge").TextContentAsync()) ?? string.Empty).Trim();

    /// <summary>The value shown beside the label "Availability". The markup carries no data-test for it.</summary>
    public async Task<string> AvailabilityAsync() =>
        (await Page.Locator($"[data-test='{ProductDetail}'] .product-detail__meta-item")
            .Filter(new LocatorFilterOptions { Has = Page.Locator(".product-detail__meta-label", new PageLocatorOptions { HasTextString = "Availability" }) })
            .Locator(".product-detail__meta-value")
            .InnerTextAsync()).Trim();

    /// <summary>The address of the product image the page shows.</summary>
    public Task<string> ImageSourceAsync() =>
        Page.Locator($"[data-test='{ProductDetail}'] img").First.EvaluateAsync<string>("e => e.currentSrc || e.getAttribute('src') || ''");
}