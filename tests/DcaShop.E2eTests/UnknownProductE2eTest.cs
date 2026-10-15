using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// The address of a product the catalogue does not have answers "not found" — the status a search engine or a link
/// checker reads, and the not-found page a visitor sees.
/// </summary>
public sealed class UnknownProductE2eTest : BaseE2eTest
{
    private const string UnknownProduct = "/products/00000000-0000-4000-8000-000000000001";

    public UnknownProductE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "An unknown product answers 404")]
    public async Task AProductTheCatalogueDoesNotHaveAnswersNotFound()
    {
        var notFound = await NotFoundPage.NavigateToAsync(Page, UnknownProduct);

        Assert.Equal(404, notFound.Status);
        Assert.True(await notFound.ShowsAsync("404"));
        Assert.True(await notFound.ShowsAsync("Page Not Found"));
        Assert.True(await notFound.ShowsAsync(
            "The page you're looking for doesn't exist or has been removed. Perhaps you were looking for one of our products?"));
    }
}