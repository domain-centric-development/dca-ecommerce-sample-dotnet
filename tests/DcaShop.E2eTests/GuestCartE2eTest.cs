using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>A guest comes back in the same browser and finds the cart as they left it.</summary>
public sealed class GuestCartE2eTest : BaseE2eTest
{
    private const string Mug = "\"Ubiquitous Language\" Mug";

    public GuestCartE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Cart kept across visits")]
    public async Task TheCartIsThereAgainWhenTheGuestComesBackInTheSameBrowser()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync(Mug);
        await product.AddToCartAsync();
        await using var nextVisit = await CloseAndReopenTheBrowserAsync();

        var cart = await CartPage.NavigateToAsync(await nextVisit.NewPageAsync());

        Assert.Equal([$"{Mug} x1"], await cart.LineItemsAsync());
    }

    /// <summary>
    /// Closing a browser ends its session cookies and keeps the ones with an expiry; the browser opened again
    /// presents only those.
    /// </summary>
    private async Task<IBrowserContext> CloseAndReopenTheBrowserAsync()
    {
        var context = Page.Context;
        var browser = context.Browser!;
        var kept = (await context.CookiesAsync())
            .Where(c => c.Expires > 0)
            .Select(c => new Cookie
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path,
                Expires = c.Expires,
                HttpOnly = c.HttpOnly,
                Secure = c.Secure,
                SameSite = c.SameSite,
            })
            .ToList();
        await context.CloseAsync();

        var reopened = await browser.NewContextAsync(ContextOptions);
        await reopened.AddCookiesAsync(kept);
        return reopened;
    }
}