using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// The "Discover products" slider on a large tablet (size l, 1024 px, inside 769 to 1180 px): four cards in view, as
/// on the desktop.
/// </summary>
public sealed class HomeSliderTabletE2eTest : BaseE2eTest
{
    public HomeSliderTabletE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    protected override BrowserNewContextOptions ContextOptions => new() { ViewportSize = new ViewportSize { Width = 1024, Height = 768 } };

    [E2eFact(DisplayName = "On a large tablet the slider shows four of its cards side by side")]
    public async Task OnALargeTabletTheFirstFourCardsStandSideBySide()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");

        Assert.Equal(8, await home.CardCountAsync());
        Assert.Equal(new[] { 0, 1, 2, 3 }, await home.CardsInViewAsync(0, 1, 2, 3));
        var positions = await home.CardPositionsAsync();
        Assert.All(positions, p => Assert.InRange(p.Top, positions[0].Top - 1, positions[0].Top + 1));
        Assert.Equal(positions.OrderBy(p => p.Left).ToList(), positions);
    }
}