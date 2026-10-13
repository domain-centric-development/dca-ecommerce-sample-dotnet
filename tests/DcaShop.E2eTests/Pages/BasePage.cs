using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>Page Object base: <c>data-test</c> selectors only, same vocabulary as the Java sample's page objects.</summary>
public abstract class BasePage
{
    protected static string BaseUrl => BrowserFixture.BaseUrl;

    protected BasePage(IPage page)
    {
        Page = page;
    }

    protected IPage Page { get; }

    protected Task WaitForUrlAsync(string pattern) => Page.WaitForURLAsync(BaseUrl + pattern);

    protected Task ClickAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").ClickAsync();

    protected Task ClickFirstAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").First.ClickAsync();

    protected Task FillAsync(string name, string value) => Page.Locator($"input[name='{name}']").FillAsync(value);

    protected Task WaitForAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").First.WaitForAsync();

    protected async Task<bool> ExistsAsync(string dataTest) => await Page.Locator($"[data-test='{dataTest}']").CountAsync() > 0;

    protected Task SelectFirstRadioAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").First.CheckAsync();

    /// <summary>True when nothing on the page reaches past the viewport, i.e. the document cannot scroll sideways.</summary>
    public Task<bool> FitsViewportAsync() =>
        Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= document.documentElement.clientWidth");

    /// <summary>The logo's wordmark in the site header, as the browser renders it.</summary>
    public async Task<string> LogoTextAsync() => (await Page.Locator("[data-test='site-header'] [data-test='site-logo']").InnerTextAsync()).Trim();

    /// <summary>Follows the logo in the site header.</summary>
    public async Task<HomePage> FollowLogoAsync()
    {
        await Page.Locator("[data-test='site-header'] [data-test='site-logo']").ClickAsync();
        return await HomePage.OpenAsync(Page);
    }

    /// <summary>The greeting in the site header as the browser renders it; empty when the header greets nobody.</summary>
    public async Task<string> HeaderGreetingAsync()
    {
        var greeting = Page.Locator("[data-test='site-header'] [data-test='user-greeting']");
        return await greeting.CountAsync() == 0 ? string.Empty : (await greeting.InnerTextAsync()).Trim();
    }

    /// <summary>Whether the site header offers the "Register" link.</summary>
    public async Task<bool> HeaderOffersRegisterAsync() =>
        await Page.Locator("[data-test='site-header'] [data-test='nav-register-link']").CountAsync() > 0;

    /// <summary>Whether the site header offers the "Login" link.</summary>
    public async Task<bool> HeaderOffersLoginAsync() =>
        await Page.Locator("[data-test='site-header'] [data-test='nav-login-link']").CountAsync() > 0;

    /// <summary>The label of the site header's logout button; empty when the header offers none.</summary>
    public async Task<string> HeaderLogoutButtonAsync()
    {
        var logout = Page.Locator("[data-test='site-header'] [data-test='logout-button']");
        return await logout.CountAsync() == 0 ? string.Empty : (await logout.InnerTextAsync()).Trim();
    }

    /// <summary>Follows the site header's "My Account" link.</summary>
    public async Task<AccountPage> FollowMyAccountAsync()
    {
        await Page.Locator("[data-test='site-header'] [data-test='nav-my-account-link']").ClickAsync();
        return await AccountPage.OpenAsync(Page);
    }

    /// <summary>
    /// Hovers the cart entry in the site header and reads the dropdown it opens. The dropdown's lines and total carry no
    /// data-test; they are read by their place in it.
    /// </summary>
    public async Task<HeaderBasket> HoverHeaderBasketAsync()
    {
        await Page.Locator("[data-test='site-header'] [data-test='mini-basket-trigger']").HoverAsync();
        var dropdown = Page.Locator("[data-test='site-header'] [data-test='mini-basket-dropdown']");
        await Assertions.Expect(dropdown).ToBeVisibleAsync();

        var items = dropdown.Locator(".mini-basket__item");
        var lines = new List<HeaderBasketLine>();
        for (var i = 0; i < await items.CountAsync(); i++)
        {
            var spans = items.Nth(i).Locator("span");
            lines.Add(new HeaderBasketLine((await spans.Nth(0).InnerTextAsync()).Trim(), (await spans.Nth(1).InnerTextAsync()).Trim()));
        }

        var total = dropdown.Locator(".mini-basket__total span");
        var viewCart = dropdown.Locator("[data-test='mini-basket-view-cart-link']");
        return new HeaderBasket(
            lines,
            (await total.Nth(0).InnerTextAsync()).Trim(),
            (await total.Nth(1).InnerTextAsync()).Trim(),
            (await viewCart.InnerTextAsync()).Trim(),
            await viewCart.GetAttributeAsync("href") ?? string.Empty);
    }

    public sealed record HeaderBasketLine(string Name, string Quantity);

    public sealed record HeaderBasket(IReadOnlyList<HeaderBasketLine> Lines, string TotalLabel, string Total, string ViewCartText, string ViewCartTarget);

    protected string CurrentPath => Page.Url.Replace(BaseUrl, string.Empty, StringComparison.Ordinal);

    protected async Task<bool> PageContainsAsync(string text) => (await Page.Locator("body").TextContentAsync() ?? string.Empty).Contains(text, StringComparison.Ordinal);
}