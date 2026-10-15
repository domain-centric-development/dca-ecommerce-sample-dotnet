using System.Text.Json;

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

    protected Task WaitForUrlAsync(string pattern) => Page.WaitForLoadedUrlAsync(BaseUrl + pattern);

    protected Task ClickAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").ClickAsync();

    protected Task ClickFirstAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").First.ClickAsync();

    protected Task FillAsync(string name, string value) => Page.Locator($"input[name='{name}']").FillAsync(value);

    protected Task WaitForAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").First.WaitForAsync();

    protected async Task<bool> ExistsAsync(string dataTest) => await Page.Locator($"[data-test='{dataTest}']").CountAsync() > 0;

    protected Task SelectFirstRadioAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").First.CheckAsync();

    /// <summary>The text of the element, waited for until it is visible, as the browser renders it.</summary>
    protected async Task<string> VisibleTextAsync(string dataTest)
    {
        var element = Page.Locator($"[data-test='{dataTest}']");
        await Assertions.Expect(element).ToBeVisibleAsync();
        return (await element.InnerTextAsync()).Trim();
    }

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

    /// <summary>
    /// The notices the page shows above its content, as the browser renders them; waits until the first one is
    /// visible.
    /// </summary>
    public async Task<IReadOnlyList<string>> NoticesAsync()
    {
        var notices = Page.Locator("[data-test='account-notice']");
        await Assertions.Expect(notices.First).ToBeVisibleAsync();
        return (await notices.AllInnerTextsAsync()).Select(notice => notice.Trim()).ToList();
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

    /// <summary>The badge on the site header's cart entry, as written; empty when the entry carries none.</summary>
    public async Task<string> HeaderCartBadgeAsync()
    {
        var badge = Page.Locator("[data-test='site-header'] [data-test='mini-basket-count']");
        return await badge.CountAsync() == 0 ? string.Empty : (await badge.InnerTextAsync()).Trim();
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

    /// <summary>Chooses a theme by its label in the footer's theme choice.</summary>
    public async Task ChooseThemeAsync(string label)
    {
        var select = Page.Locator("[data-test='site-footer'] [data-test='theme-select']");
        await Assertions.Expect(select).ToBeVisibleAsync();
        await select.SelectOptionAsync(new SelectOptionValue { Label = label });
    }

    /// <summary>The label of the theme the footer's theme choice shows selected.</summary>
    public async Task<string> SelectedThemeAsync() =>
        (await Page.Locator("[data-test='site-footer'] [data-test='theme-select'] option:checked").InnerTextAsync()).Trim();

    /// <summary>The theme the page is shown in, as the document carries it; null for the default theme.</summary>
    public Task<string?> ShownThemeAsync() => Page.Locator("html").GetAttributeAsync("data-theme");

    /// <summary>Marks the loaded document, so that a later check can tell whether the page was reloaded since.</summary>
    public Task MarkDocumentAsync() => Page.EvaluateAsync("() => { window.__documentMark = true; }");

    /// <summary>Whether the document marked by <see cref="MarkDocumentAsync"/> is still the one shown.</summary>
    public Task<bool> IsMarkedDocumentAsync() => Page.EvaluateAsync<bool>("() => window.__documentMark === true");

    /// <summary>Whether this browser can animate the change from one page of the shop to the next.</summary>
    public Task<bool> SupportsPageTransitionsAsync() => Page.EvaluateAsync<bool>("() => 'onpagereveal' in window");

    /// <summary>
    /// From the next page on, records the animations of the transition that reveals each page — every animation of a
    /// view-transition part that lasts longer than nothing, once the transition is ready to run.
    /// </summary>
    public Task RecordPageTransitionsAsync() => Page.AddInitScriptAsync("""
        addEventListener('pagereveal', async (event) => {
          const transition = event.viewTransition;
          if (transition) {
            // A transition the browser skips (under reduced motion, say) rejects `ready`: expected, not a failure.
            try { await transition.ready; } catch { }
          }
          window.__pageTransitionAnimations = document.getAnimations()
            .filter(a => (a.effect?.pseudoElement ?? '').startsWith('::view-transition'))
            .map(a => ({ part: a.effect.pseudoElement, duration: a.effect.getComputedTiming().duration }))
            .filter(a => a.duration > 0)
            .map(a => `${a.part} ${a.duration} ms`);
          window.__pageTransitionParts = JSON.stringify(document.getAnimations()
            .filter(a => (a.effect?.pseudoElement ?? '').startsWith('::view-transition'))
            .map(a => ({ part: a.effect.pseudoElement, frames: a.effect.getKeyframes() })));
        });
        """);

    /// <summary>The animations recorded for the transition that revealed the shown page; empty when nothing moved.</summary>
    public async Task<IReadOnlyList<string>> PageTransitionAnimationsAsync()
    {
        await Page.WaitForFunctionAsync("() => Array.isArray(window.__pageTransitionAnimations)");
        return await Page.EvaluateAsync<string[]>("() => window.__pageTransitionAnimations");
    }

    /// <summary>
    /// The animated parts of the transition that revealed the shown page, each with the values of its first and its last
    /// keyframe — what the part looked like, and where it stood, when the animation started and when it ended.
    /// </summary>
    public async Task<IReadOnlyList<PageTransitionPart>> PageTransitionPartsAsync()
    {
        await Page.WaitForFunctionAsync("() => typeof window.__pageTransitionParts === 'string'");
        using var parts = JsonDocument.Parse(await Page.EvaluateAsync<string>("() => window.__pageTransitionParts"));
        return parts.RootElement.EnumerateArray()
            .Select(part =>
            {
                var frames = part.GetProperty("frames").EnumerateArray().Select(KeyframeValues).ToList();
                return new PageTransitionPart(
                    part.GetProperty("part").GetString() ?? string.Empty,
                    frames.FirstOrDefault() ?? new Dictionary<string, string>(),
                    frames.LastOrDefault() ?? new Dictionary<string, string>());
            })
            .ToList();
    }

    /// <summary>Waits until the transition that revealed the shown page has run to its end.</summary>
    public Task WaitForPageTransitionToEndAsync() => Page.WaitForFunctionAsync("""
        () => typeof window.__pageTransitionParts === 'string'
          && !document.getAnimations().some(a => (a.effect?.pseudoElement ?? '').startsWith('::view-transition'))
        """);

    private static IReadOnlyDictionary<string, string> KeyframeValues(JsonElement keyframe) =>
        keyframe.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.ToString());

    /// <summary>One animated part of a page transition, e.g. <c>::view-transition-group(site-header)</c>.</summary>
    public sealed record PageTransitionPart(string Part, IReadOnlyDictionary<string, string> First, IReadOnlyDictionary<string, string> Last)
    {
        public string? FirstValue(string property) => First.GetValueOrDefault(property);

        public string? LastValue(string property) => Last.GetValueOrDefault(property);
    }

    public sealed record HeaderBasketLine(string Name, string Quantity);

    public sealed record HeaderBasket(IReadOnlyList<HeaderBasketLine> Lines, string TotalLabel, string Total, string ViewCartText, string ViewCartTarget);

    protected string CurrentPath => Page.Url.Replace(BaseUrl, string.Empty, StringComparison.Ordinal);
}