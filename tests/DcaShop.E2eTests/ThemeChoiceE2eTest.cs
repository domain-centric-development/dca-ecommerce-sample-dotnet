using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>The theme a visitor chooses in the footer: applied at once, kept by this browser across pages and visits.</summary>
public sealed class ThemeChoiceE2eTest : BaseE2eTest
{
    public ThemeChoiceE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Choosing a theme applies at once")]
    public async Task ChoosingOrganicShowsThePageInOrganicWithoutAReload()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        Assert.Null(await catalog.ShownThemeAsync());
        await catalog.MarkDocumentAsync();

        await catalog.ChooseThemeAsync("Organic");

        Assert.Equal("organic", await catalog.ShownThemeAsync());
        Assert.True(await catalog.IsMarkedDocumentAsync(), "the page was not reloaded");
    }

    [E2eFact(DisplayName = "The choice follows to other pages")]
    public async Task AThemeChosenOnTheCatalogueIsShownOnTheCartPage()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        await catalog.ChooseThemeAsync("Neon + Dark");

        var cart = await CartPage.NavigateToAsync(Page);

        Assert.Equal("vibrant", await cart.ShownThemeAsync());
        Assert.Equal("Neon + Dark", await cart.SelectedThemeAsync());
    }

    [E2eFact(DisplayName = "The choice survives a new visit")]
    public async Task AThemeChosenBeforeClosingTheShopIsShownWhenItIsOpenedAgain()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        await catalog.ChooseThemeAsync("Classic");
        await Page.CloseAsync();

        var home = await HomePage.NavigateToAsync(await NewPageInThisBrowserAsync());

        Assert.Equal("classic", await home.ShownThemeAsync());
        Assert.Equal("Classic", await home.SelectedThemeAsync());
    }

    [E2eFact(DisplayName = "Choosing blueprint again returns to the default")]
    public async Task ChoosingBlueprintAfterAnotherThemeShowsTheCatalogueInBlueprintAgain()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        await catalog.ChooseThemeAsync("Sharp & Minimal");

        await catalog.ChooseThemeAsync("Blueprint");
        var reopened = await ProductCatalogPage.NavigateToAsync(Page);

        Assert.Null(await reopened.ShownThemeAsync());
    }

    [E2eFact(DisplayName = "Another browser keeps the default")]
    public async Task AThemeChosenInOneBrowserLeavesAnotherBrowserInBlueprint()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        await catalog.ChooseThemeAsync("Organic");

        await using var otherBrowser = await NewBrowserAsync();
        var otherCatalog = await ProductCatalogPage.NavigateToAsync(await otherBrowser.NewPageAsync());

        Assert.Null(await otherCatalog.ShownThemeAsync());
    }
}