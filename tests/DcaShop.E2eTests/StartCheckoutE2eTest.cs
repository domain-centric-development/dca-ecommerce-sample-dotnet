using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest starts the checkout straight from the cart, without an account.</summary>
public sealed class StartCheckoutE2eTest : BaseE2eTest
{
    public StartCheckoutE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Proceed opens the buyer step")]
    public async Task PressingProceedToCheckoutOpensTheBuyerStepWithoutAskingForAnAccount()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");
        await product.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var visited = new List<string>();
        Page.Request += (_, request) =>
        {
            if (request.IsNavigationRequest)
            {
                visited.Add(new Uri(request.Url).AbsolutePath);
            }
        };

        await cart.PressProceedToCheckoutAsync();

        Assert.Equal("/checkout/buyer", cart.ShownPath);
        Assert.DoesNotContain(visited, path => path.StartsWith("/login", StringComparison.Ordinal) || path.StartsWith("/register", StringComparison.Ordinal));
    }
}