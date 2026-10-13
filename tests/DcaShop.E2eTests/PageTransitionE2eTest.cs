using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// Moving from page to page is animated where the browser can animate it: the content fades, the shop's frame stays,
/// a product's image travels from its card to the product page and the checkout progress bar holds its place. What
/// the browser animates is read from the parts of the transition that reveals the next page.
/// </summary>
public sealed class PageTransitionE2eTest : BaseE2eTest
{
    private const string Product = "Domain-Driven Design";

    public PageTransitionE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    /// <summary>Tall enough that a whole checkout step is in sight, so pressing its button scrolls nothing.</summary>
    protected override BrowserNewContextOptions ContextOptions => new() { ViewportSize = new ViewportSize { Width = 1280, Height = 1800 } };

    [E2eFact(DisplayName = "The frame stays while the page changes")]
    public async Task FollowingViewDetailsFadesTheContentWhileHeaderLogoAndFooterStay()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        Assert.True(await catalog.SupportsPageTransitionsAsync(), "the browser cannot animate page changes");
        await catalog.RecordPageTransitionsAsync();

        var product = await catalog.ViewProductAsync(Product);
        var parts = await product.PageTransitionPartsAsync();

        var leaving = Part(parts, "::view-transition-old(root)");
        Assert.Equal(("1", "0"), (leaving.FirstValue("opacity"), leaving.LastValue("opacity")));
        var arriving = Part(parts, "::view-transition-new(root)");
        Assert.Equal(("0", "1"), (arriving.FirstValue("opacity"), arriving.LastValue("opacity")));
        foreach (var frame in new[] { "site-header", "site-logo" })
        {
            var group = Part(parts, $"::view-transition-group({frame})");
            Assert.NotNull(group.FirstValue("transform"));
            Assert.Equal(group.FirstValue("transform"), group.LastValue("transform"));
        }
        Part(parts, "::view-transition-group(site-footer)");
        Assert.DoesNotContain(parts, part => part.Part is "::view-transition-old(site-header)" or "::view-transition-new(site-header)"
            or "::view-transition-old(site-footer)" or "::view-transition-new(site-footer)");
    }

    [E2eFact(DisplayName = "The product image carries over")]
    public async Task FollowingViewDetailsMovesTheCardsImageToItsPlaceOnTheProductPage()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        Assert.True(await catalog.SupportsPageTransitionsAsync(), "the browser cannot animate page changes");
        await catalog.RecordPageTransitionsAsync();

        var product = await catalog.ViewProductAsync(Product);
        var parts = await product.PageTransitionPartsAsync();

        var image = Assert.Single(parts, part => part.Part.StartsWith("::view-transition-group(product-img-", StringComparison.Ordinal));
        Assert.NotEqual(image.FirstValue("transform"), image.LastValue("transform"));
    }

    [E2eFact(DisplayName = "The checkout progress stays anchored")]
    public async Task ContinuingToTheDeliveryStepKeepsTheProgressBarInPlaceWhileTheFormChanges()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        Assert.True(await catalog.SupportsPageTransitionsAsync(), "the browser cannot animate page changes");
        await catalog.RecordPageTransitionsAsync();
        await (await catalog.ViewProductAsync(Product)).AddToCartAsync();
        var buyer = await (await CartPage.NavigateToAsync(Page)).ProceedToCheckoutAsync();
        await buyer.WaitForPageTransitionToEndAsync();

        var delivery = await (await buyer.FillBuyerInfoAsync("guest@example.com", "Test", "Guest", "+1-555-0100")).ContinueToDeliveryAsync();
        var parts = await delivery.PageTransitionPartsAsync();

        var progress = Part(parts, "::view-transition-group(checkout-progress)");
        Assert.NotNull(progress.FirstValue("transform"));
        Assert.Equal(progress.FirstValue("transform"), progress.LastValue("transform"));
        var arriving = Part(parts, "::view-transition-new(root)");
        Assert.Equal(("0", "1"), (arriving.FirstValue("opacity"), arriving.LastValue("opacity")));
    }

    private static BasePage.PageTransitionPart Part(IReadOnlyList<BasePage.PageTransitionPart> parts, string name) =>
        parts.FirstOrDefault(part => part.Part == name)
        ?? throw new Xunit.Sdk.XunitException($"no animated part {name} among: {string.Join(", ", parts.Select(Describe))}");

    private static string Describe(BasePage.PageTransitionPart part) =>
        $"{part.Part} [{string.Join(' ', part.First.Select(v => $"{v.Key}={v.Value}"))}] -> [{string.Join(' ', part.Last.Select(v => $"{v.Key}={v.Value}"))}]";
}