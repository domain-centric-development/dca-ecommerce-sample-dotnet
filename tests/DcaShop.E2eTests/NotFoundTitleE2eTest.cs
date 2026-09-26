using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// A product address no product has shows the not-found page, which still names the shop in the browser tab.
/// </summary>
public sealed class NotFoundTitleE2eTest : BaseE2eTest
{
    public NotFoundTitleE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Unknown product shows the not-found page")]
    public async Task NotFoundPageIsTitledWithTheShopName()
    {
        var notFound = await NotFoundPage.NavigateToAsync(Page, "/products/no-such-product");

        Assert.Equal("domaincentric.commerce", await notFound.DocumentTitleAsync());
        Assert.True(await notFound.ShowsAsync("404"));
        Assert.True(await notFound.ShowsAsync("Page Not Found"));
        Assert.True(await notFound.ShowsAsync(
            "The page you're looking for doesn't exist or has been removed. Perhaps you were looking for one of our products?"));
        Assert.Equal(("Browse All Products", "/products"), await notFound.BrowseLinkAsync());
        Assert.Equal(("Go to Homepage", "/"), await notFound.HomeLinkAsync());
    }
}