using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>A visitor whose device asks for less motion changes pages without any animation.</summary>
public sealed class ReducedMotionE2eTest : BaseE2eTest
{
    public ReducedMotionE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    protected override BrowserNewContextOptions ContextOptions => new() { ReducedMotion = ReducedMotion.Reduce };

    [E2eFact(DisplayName = "Reduced motion means no animation")]
    public async Task TheProductPageAppearsWithoutAnimationWhenTheVisitorPrefersReducedMotion()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        Assert.True(await catalog.SupportsPageTransitionsAsync(), "the browser cannot animate page changes");
        await catalog.RecordPageTransitionsAsync();

        var product = await catalog.ViewProductAsync("Domain-Driven Design");

        Assert.Empty(await product.PageTransitionAnimationsAsync());
    }
}