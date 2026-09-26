using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>The site header's logo carries the shop's name and leads home from any page.</summary>
public sealed class SiteHeaderE2eTest : BaseE2eTest
{
    public SiteHeaderE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Logo leads home")]
    public async Task TheLogoLeadsFromAProductPageToTheHomePage()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");
        Assert.Equal("domaincentric.commerce", await product.LogoTextAsync());

        var home = await product.FollowLogoAsync();

        Assert.Equal("Welcome to domaincentric.commerce", await home.HeadingAsync());
    }
}