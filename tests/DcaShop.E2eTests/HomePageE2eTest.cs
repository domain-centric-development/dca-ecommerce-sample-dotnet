using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// The home page greets a visitor with what the shop is: its heading, the subtitle and the line below it.
/// </summary>
public sealed class HomePageE2eTest : BaseE2eTest
{
    public HomePageE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Home page welcomes the visitor")]
    public async Task TheHomePageSaysWhatTheShopSells()
    {
        var home = await HomePage.NavigateToAsync(Page);

        Assert.Equal("Welcome to domaincentric.commerce", await home.HeadingAsync());
        Assert.Equal("Books, modelling supplies and hexagon merchandise for people who draw boundaries", await home.SubtitleAsync());
        Assert.Equal(
            "Everything this architecture is made of: the books it grew out of, the supplies a design workshop runs on, "
            + "and hexagons for your desk.",
            await home.DescriptionAsync());
    }
}