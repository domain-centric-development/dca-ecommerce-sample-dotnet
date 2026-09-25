using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// The "Discover products" slider on the homepage, in the suite's default browser window: where it stands, what it
/// holds, what a card shows and where it leads.
/// </summary>
public sealed class HomeSliderE2eTest : BaseE2eTest
{
    public HomeSliderE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "The homepage shows a \"Discover products\" slider directly below the hero")]
    public async Task ShowsTheDiscoverProductsSliderDirectlyBelowTheHero()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");

        Assert.Equal("Discover products", await home.SliderTitleAsync());
        Assert.Equal("product-slider", await home.SectionAfterHeroAsync());
        Assert.True(await home.SliderStandsBetweenHeroAndAsync("features"), "the slider stands below the hero and before \"Why Shop With Us\"");
    }

    [E2eFact(DisplayName = "The homepage slider holds eight different products of the sample catalog")]
    public async Task HoldsEightDifferentProductsOfTheSampleCatalog()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");
        var names = await home.CardNamesAsync();

        Assert.Equal(8, await home.CardCountAsync());
        Assert.Equal(8, names.Distinct(StringComparer.Ordinal).Count());
        var catalog = await (await ProductCatalogPage.NavigateToAsync(Page)).ProductTitlesAsync();
        Assert.All(names, name => Assert.Contains(name, catalog));
    }

    [E2eFact(DisplayName = "The homepage slider draws its products anew on every request")]
    public async Task DrawsItsProductsAnewOnEveryRequest()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");
        var noted = (await home.CardNamesAsync()).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(8, noted.Count);

        var differentSelections = 0;
        for (var reload = 0; reload < 10; reload++)
        {
            await home.ReloadAsync();
            Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");
            if (!noted.SetEquals(await home.CardNamesAsync()))
            {
                differentSelections++;
            }
        }

        Assert.True(differentSelections >= 1, "ten reloads all showed the same eight products");
    }

    [E2eFact(DisplayName = "A slider card shows the product's image, name and price")]
    public async Task ACardShowsTheProductsImageNameAndPrice()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");
        var name = await home.CardNameAsync(0);
        var price = await home.CardPriceAsync(0);
        var image = await home.CardImageSourceAsync(0);
        Assert.True(await home.CardImageIsLoadedAsync(0), "the card shows the product's image");

        var product = await ProductDetailPage.NavigateToAsync(Page, await home.CardLinkPathAsync(0));

        Assert.Equal(await product.TitleAsync(), name);
        Assert.Equal(await product.PriceAsync(), price);
        Assert.Equal(await product.ImageSourceAsync(), image);
    }

    [E2eFact(DisplayName = "A slider card leads to its product page")]
    public async Task ACardLeadsToItsProductPage()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");
        var name = await home.CardNameAsync(1);
        var link = await home.CardLinkPathAsync(1);
        Assert.Matches("^/products/[0-9a-f-]{36}$", link);

        var product = await home.FollowCardAsync(1);

        Assert.Equal(link, product.ShownPath);
        Assert.Equal(name, await product.TitleAsync());
    }

    [E2eFact(DisplayName = "On the desktop the slider shows four of its cards side by side")]
    public async Task OnTheDesktopTheFirstFourCardsStandSideBySide()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");

        Assert.Equal(8, await home.CardCountAsync());
        Assert.Equal(new[] { 0, 1, 2, 3 }, await home.CardsInViewAsync(0, 1, 2, 3));
        var positions = await home.CardPositionsAsync();
        Assert.All(positions, p => Assert.InRange(p.Top, positions[0].Top - 1, positions[0].Top + 1));
        Assert.Equal(positions.OrderBy(p => p.Left).ToList(), positions);
        Assert.True(await home.IsPreviousDisabledAsync(), "\"Previous\" is disabled");
        Assert.True(await home.IsNextEnabledAsync(), "\"Next\" is enabled");
    }

    [E2eFact(DisplayName = "On the desktop Next moves the slider on by one card")]
    public async Task OnTheDesktopNextMovesTheSliderOnByOneCard()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");
        Assert.Equal(new[] { 0, 1, 2, 3 }, await home.CardsInViewAsync(0, 1, 2, 3));
        Assert.True(await home.IsNextEnabledAsync(), "\"Next\" can be pressed");

        await home.PressNextAsync();

        Assert.Equal(new[] { 1, 2, 3, 4 }, await home.CardsInViewAsync(1, 2, 3, 4));
        Assert.True(await home.IsPreviousEnabledAsync(), "\"Previous\" is enabled");
    }

    [E2eFact(DisplayName = "On the desktop the slider stops at its last card")]
    public async Task OnTheDesktopTheSliderStopsAtItsLastCard()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");

        for (var press = 1; press <= 4; press++)
        {
            Assert.True(await home.IsNextEnabledAsync(), $"\"Next\" can be pressed a {press}. time");
            await home.PressNextAsync();
            var expected = Enumerable.Range(press, 4).ToArray();
            Assert.Equal(expected, await home.CardsInViewAsync(expected));
        }

        Assert.Equal(new[] { 4, 5, 6, 7 }, await home.CardsInViewAsync(4, 5, 6, 7));
        Assert.True(await home.IsNextDisabledAsync(), "\"Next\" is disabled at the last card");
    }
}