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

    [E2eFact(DisplayName = "The homepage slider holds four different products of the sample catalog")]
    public async Task HoldsFourDifferentProductsOfTheSampleCatalog()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");
        var names = await home.CardNamesAsync();

        Assert.Equal(4, await home.CardCountAsync());
        Assert.Equal(4, names.Distinct(StringComparer.Ordinal).Count());
        var catalog = await (await ProductCatalogPage.NavigateToAsync(Page)).ProductTitlesAsync();
        Assert.All(names, name => Assert.Contains(name, catalog));
    }

    [E2eFact(DisplayName = "The homepage slider draws its products anew on every request")]
    public async Task DrawsItsProductsAnewOnEveryRequest()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");
        var noted = (await home.CardNamesAsync()).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(4, noted.Count);

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

        Assert.True(differentSelections >= 1, "ten reloads all showed the same four products");
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

    [E2eFact(DisplayName = "On the desktop the slider shows its four cards side by side")]
    public async Task OnTheDesktopTheFourCardsStandSideBySide()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");

        Assert.Equal(new[] { 0, 1, 2, 3 }, await home.CardsInViewAsync(0, 1, 2, 3));
        var positions = await home.CardPositionsAsync();
        Assert.All(positions, p => Assert.InRange(p.Top, positions[0].Top - 1, positions[0].Top + 1));
        Assert.Equal(positions.OrderBy(p => p.Left).ToList(), positions);
        Assert.True(await home.IsPreviousDisabledAsync(), "\"Previous\" is disabled");
        Assert.True(await home.IsNextDisabledAsync(), "\"Next\" is disabled");
    }
}