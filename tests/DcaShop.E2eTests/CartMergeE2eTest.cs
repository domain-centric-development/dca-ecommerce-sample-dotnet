using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// The cart merge flow through the browser: a visitor with items in both their guest cart and their account
/// cart is asked which one to keep. Port of the Java sample's <c>CartMergeE2ETest</c>.
/// </summary>
public sealed class CartMergeE2eTest : BaseE2eTest
{
    private const string TestPassword = "SecurePassword123!";

    public CartMergeE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Full flow: login with both carts, see merge options, select merge, verify combined cart")]
    public async Task FullCartMergeFlow()
    {
        // An account with a cart of its own
        var email = $"merge-test-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync(email, TestPassword);
        await AddProductToCartAsync();

        var cart = await CartPage.NavigateToAsync(Page);
        Assert.True(await cart.ItemCountAsync() > 0, "Account cart should have items");

        // The same browser as a stranger: a guest cart with items of its own
        await ClearCookiesAsync();
        await AddProductToCartAsync();
        cart = await CartPage.NavigateToAsync(Page);
        Assert.True(await cart.ItemCountAsync() > 0, "Anonymous cart should have items");

        // Logging in now leaves a decision to make
        var login = await LoginPage.NavigateToAsync(Page);
        await login.LoginAsync(email, TestPassword);

        var merge = new CartMergePage(Page);
        Assert.True(merge.IsOnMergePage, $"Should be on the merge page. Got: {CurrentPath}");
        Assert.True(await merge.ShowsAnonymousCartAsync(), "Should show anonymous cart summary");
        Assert.True(await merge.ShowsAccountCartAsync(), "Should show account cart summary");
        Assert.True(await merge.ShowsMergeOptionsAsync(), "Should show all merge options");

        cart = await merge.MergeBothCartsAsync();

        Assert.True(await cart.HasItemsAsync(), "Merged cart should have items");
        Assert.StartsWith("/cart", CurrentPath, StringComparison.Ordinal);
    }

    private async Task AddProductToCartAsync()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var detail = await catalog.ViewFirstProductAsync();
        await detail.AddToCartAsync();
    }
}
