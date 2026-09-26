using System.Text.RegularExpressions;

using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>
/// The homepage and its "Discover products" slider. A card is <em>in view</em> when the whole card lies inside the
/// slider as the browser lays it out and inside the window's width — judged from the rendered boxes, never from class
/// names or markup.
/// </summary>
public sealed class HomePage : BasePage
{
    private const string Hero = "hero";
    private const string Slider = "product-slider";
    private const string SliderTitle = "product-slider-title";
    private const string Card = "product-slider-card";
    private const string CardImage = "product-slider-card-image";
    private const string CardName = "product-slider-card-name";
    private const string CardPrice = "product-slider-card-price";
    private const string CardLink = "product-slider-card-link";
    private const string Previous = "product-slider-previous";
    private const string Next = "product-slider-next";

    /// <summary>How long a paging step may take to settle (smooth scrolling) before a check gives up.</summary>
    private const float SettleTimeoutMs = 5_000;

    private const string CardInView = """
        ([slider, card, index]) => {
            const s = document.querySelector(`[data-test='${slider}']`);
            const c = s && s.querySelectorAll(`[data-test='${card}']`)[index];
            if (!c) return false;
            const sb = s.getBoundingClientRect();
            const cb = c.getBoundingClientRect();
            const left = Math.max(sb.left, 0) - 1;
            const right = Math.min(sb.right, document.documentElement.clientWidth) + 1;
            return cb.width > 0 && cb.left >= left && cb.right <= right;
        }
        """;

    private HomePage(IPage page) : base(page)
    {
    }

    public static async Task<HomePage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + "/");
        var home = new HomePage(page);
        await home.WaitForAsync(Hero);
        return home;
    }

    /// <summary>The home page reached by following a link: waits for its address and its hero.</summary>
    public static async Task<HomePage> OpenAsync(IPage page)
    {
        var home = new HomePage(page);
        await home.WaitForUrlAsync("/");
        await home.WaitForAsync(Hero);
        return home;
    }

    public async Task ReloadAsync()
    {
        await Page.ReloadAsync();
        await WaitForAsync(Hero);
    }

    /// <summary>
    /// Waits for the slider and scrolls it into the window, as a shopper would before looking at it; false when the
    /// page shows no slider.
    /// </summary>
    public async Task<bool> ShowSliderAsync()
    {
        var slider = Locator(Slider);
        try
        {
            await slider.WaitForAsync(new LocatorWaitForOptions { Timeout = SettleTimeoutMs });
        }
        catch (TimeoutException)
        {
            return false;
        }

        await slider.ScrollIntoViewIfNeededAsync();
        return true;
    }

    /// <summary>The hero's heading as the browser renders it.</summary>
    public async Task<string> HeadingAsync() =>
        (await Locator(Hero).GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Level = 1 }).InnerTextAsync()).Trim();

    /// <summary>The wording of the hero's first paragraph, directly below the heading — the stylesheet's casing aside.</summary>
    public Task<string> SubtitleAsync() => HeroParagraphTextAsync(0);

    /// <summary>The wording of the hero's second paragraph, below the subtitle.</summary>
    public Task<string> DescriptionAsync() => HeroParagraphTextAsync(1);

    private async Task<string> HeroParagraphTextAsync(int index) =>
        Regex.Replace(await Locator(Hero).Locator("p").Nth(index).TextContentAsync() ?? "", @"\s+", " ").Trim();

    public async Task<string> SliderTitleAsync() => (await Locator(SliderTitle).InnerTextAsync()).Trim();

    /// <summary>The <c>data-test</c> of the section that follows the hero on the page.</summary>
    public Task<string?> SectionAfterHeroAsync() =>
        Page.EvaluateAsync<string?>(
            "hero => document.querySelector(`[data-test='${hero}']`)?.nextElementSibling?.getAttribute('data-test') ?? null",
            Hero);

    /// <summary>True when the slider is laid out below the hero and above the section of the given <c>data-test</c>.</summary>
    public async Task<bool> SliderStandsBetweenHeroAndAsync(string followingSection)
    {
        var hero = await Locator(Hero).BoundingBoxAsync();
        var slider = await Locator(Slider).BoundingBoxAsync();
        var following = await Locator(followingSection).BoundingBoxAsync();
        return hero is not null && slider is not null && following is not null
               && slider.Y >= hero.Y + hero.Height - 1
               && slider.Y + slider.Height <= following.Y + 1;
    }

    public Task<int> CardCountAsync() => Cards.CountAsync();

    /// <summary>The product names on the cards, in slider order.</summary>
    public async Task<IReadOnlyList<string>> CardNamesAsync() =>
        (await Page.Locator($"[data-test='{Slider}'] [data-test='{CardName}']").AllInnerTextsAsync()).Select(n => n.Trim()).ToList();

    public async Task<string> CardNameAsync(int index) => (await Cards.Nth(index).Locator($"[data-test='{CardName}']").InnerTextAsync()).Trim();

    public async Task<string> CardPriceAsync(int index) => (await Cards.Nth(index).Locator($"[data-test='{CardPrice}']").InnerTextAsync()).Trim();

    /// <summary>The image the card shows: the address the browser loaded, empty when it shows no picture.</summary>
    public async Task<string> CardImageSourceAsync(int index)
    {
        var image = Cards.Nth(index).Locator($"[data-test='{CardImage}']");
        return await image.EvaluateAsync<string>("e => e.currentSrc || e.getAttribute('src') || ''");
    }

    public async Task<bool> CardImageIsLoadedAsync(int index) =>
        await Cards.Nth(index).Locator($"[data-test='{CardImage}']")
            .EvaluateAsync<bool>("async e => { if (!e.complete) await e.decode().catch(() => {}); return e.complete && e.naturalWidth > 0; }");

    /// <summary>The path the card's link leads to.</summary>
    public async Task<string> CardLinkPathAsync(int index) =>
        await Cards.Nth(index).Locator($"[data-test='{CardLink}']").GetAttributeAsync("href") ?? string.Empty;

    public async Task<ProductDetailPage> FollowCardAsync(int index)
    {
        await Cards.Nth(index).Locator($"[data-test='{CardLink}']").ClickAsync();
        return await ProductDetailPage.OpenAsync(Page);
    }

    /// <summary>The top and left edge of every card, in slider order.</summary>
    public async Task<IReadOnlyList<(float Left, float Top)>> CardPositionsAsync()
    {
        var positions = new List<(float, float)>();
        for (var i = 0; i < await CardCountAsync(); i++)
        {
            var box = await Cards.Nth(i).BoundingBoxAsync();
            positions.Add(box is null ? (float.NaN, float.NaN) : (box.X, box.Y));
        }
        return positions;
    }

    /// <summary>Whether the card is in view right now.</summary>
    public Task<bool> IsCardInViewAsync(int index) => Page.EvaluateAsync<bool>(CardInView, new object[] { Slider, Card, index });

    /// <summary>The indexes of the cards in view once paging has settled: waits until exactly <paramref name="expected"/> is in view.</summary>
    public async Task<IReadOnlyList<int>> CardsInViewAsync(params int[] expected)
    {
        try
        {
            await Page.WaitForFunctionAsync(
                $"args => {{ const inView = {CardInView}; const n = document.querySelectorAll(`[data-test='${{args[0]}}'] [data-test='${{args[1]}}']`).length; "
                + "const shown = []; for (let i = 0; i < n; i++) if (inView([args[0], args[1], i])) shown.push(i); "
                + "return JSON.stringify(shown) === JSON.stringify(args[2]); }",
                new object[] { Slider, Card, expected },
                new PageWaitForFunctionOptions { Timeout = SettleTimeoutMs });
        }
        catch (TimeoutException)
        {
            // Settled elsewhere: report what is in view now, so the assertion names it.
        }

        var shown = new List<int>();
        for (var i = 0; i < await CardCountAsync(); i++)
        {
            if (await IsCardInViewAsync(i))
            {
                shown.Add(i);
            }
        }
        return shown;
    }

    public Task PressNextAsync() => Locator(Next).ClickAsync();

    public Task PressPreviousAsync() => Locator(Previous).ClickAsync();

    public Task<bool> IsNextDisabledAsync() => SettlesDisabledAsync(Next);

    public Task<bool> IsPreviousDisabledAsync() => SettlesDisabledAsync(Previous);

    public Task<bool> IsNextEnabledAsync() => SettlesEnabledAsync(Next);

    public Task<bool> IsPreviousEnabledAsync() => SettlesEnabledAsync(Previous);

    /// <summary>Presses Tab until the keyboard focus is on "Next"; false when it never gets there.</summary>
    public async Task<bool> TabToNextAsync()
    {
        for (var press = 0; press < 200; press++)
        {
            await Page.Keyboard.PressAsync("Tab");
            var focused = await Page.EvaluateAsync<string?>("() => document.activeElement?.getAttribute('data-test') ?? null");
            if (focused == Next)
            {
                return true;
            }
        }
        return false;
    }

    public Task PressEnterAsync() => Page.Keyboard.PressAsync("Enter");

    private ILocator Cards => Page.Locator($"[data-test='{Slider}'] [data-test='{Card}']");

    private ILocator Locator(string dataTest) => Page.Locator($"[data-test='{dataTest}']");

    /// <summary>The button's state once paging has settled: waits for it to be disabled, answers false if it stays enabled.</summary>
    private async Task<bool> SettlesDisabledAsync(string dataTest)
    {
        try
        {
            await Page.WaitForFunctionAsync(
                "dt => document.querySelector(`[data-test='${dt}']`)?.disabled === true",
                dataTest,
                new PageWaitForFunctionOptions { Timeout = SettleTimeoutMs });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>The button's state once paging has settled: waits for it to be enabled, answers false if it stays disabled.</summary>
    private async Task<bool> SettlesEnabledAsync(string dataTest)
    {
        try
        {
            await Page.WaitForFunctionAsync(
                "dt => document.querySelector(`[data-test='${dt}']`)?.disabled === false",
                dataTest,
                new PageWaitForFunctionOptions { Timeout = SettleTimeoutMs });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}