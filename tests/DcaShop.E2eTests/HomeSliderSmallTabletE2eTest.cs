using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// The "Discover products" slider on a small tablet (size m, 768 px, the upper edge of 481 to 768 px): two cards in
/// view side by side.
/// </summary>
public sealed class HomeSliderSmallTabletE2eTest : BaseE2eTest
{
    public HomeSliderSmallTabletE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    protected override BrowserNewContextOptions ContextOptions => new() { ViewportSize = new ViewportSize { Width = 768, Height = 1024 } };

    [E2eFact(DisplayName = "On a small tablet the slider shows two of its cards side by side")]
    public async Task OnASmallTabletTheFirstTwoCardsStandSideBySide()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");

        Assert.Equal(8, await home.CardCountAsync());
        Assert.Equal(new[] { 0, 1 }, await home.CardsInViewAsync(0, 1));
        var positions = await home.CardPositionsAsync();
        Assert.All(positions, p => Assert.InRange(p.Top, positions[0].Top - 1, positions[0].Top + 1));
        Assert.Equal(positions.OrderBy(p => p.Left).ToList(), positions);
    }
}