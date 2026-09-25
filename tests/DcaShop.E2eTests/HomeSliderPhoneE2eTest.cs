using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// The "Discover products" slider on a phone (393 px, as the shop's other phone checks): one card in view, paged by
/// "Previous" and "Next" — by mouse or keyboard — and still unless the shopper pages it.
/// </summary>
public sealed class HomeSliderPhoneE2eTest : BaseE2eTest
{
    public HomeSliderPhoneE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    protected override BrowserNewContextOptions ContextOptions => new() { ViewportSize = new ViewportSize { Width = 393, Height = 852 } };

    [E2eFact(DisplayName = "On a phone the slider shows one card at a time")]
    public async Task OnAPhoneOnlyTheFirstCardIsInView()
    {
        var home = await OpenSliderAsync();

        Assert.Equal(new[] { 0 }, await home.CardsInViewAsync(0));
        Assert.True(await home.IsPreviousDisabledAsync(), "\"Previous\" is disabled at the first card");
    }

    [E2eFact(DisplayName = "On a phone Next brings the following slider card into view")]
    public async Task OnAPhoneNextBringsTheSecondCardIntoView()
    {
        var home = await OpenSliderAsync();
        Assert.Equal(new[] { 0 }, await home.CardsInViewAsync(0));

        await home.PressNextAsync();

        Assert.Equal(new[] { 1 }, await home.CardsInViewAsync(1));
    }

    [E2eFact(DisplayName = "On a phone Previous brings the preceding slider card back into view")]
    public async Task OnAPhonePreviousBringsTheFirstCardBackIntoView()
    {
        var home = await OpenSliderAsync();
        await home.PressNextAsync();
        Assert.Equal(new[] { 1 }, await home.CardsInViewAsync(1));

        await home.PressPreviousAsync();

        Assert.Equal(new[] { 0 }, await home.CardsInViewAsync(0));
    }

    [E2eFact(DisplayName = "On a phone the slider's Next button works from the keyboard")]
    public async Task OnAPhoneNextWorksFromTheKeyboard()
    {
        var home = await OpenSliderAsync();
        Assert.Equal(new[] { 0 }, await home.CardsInViewAsync(0));
        Assert.True(await home.TabToNextAsync(), "the Tab key reaches \"Next\"");

        await home.PressEnterAsync();

        Assert.Equal(new[] { 1 }, await home.CardsInViewAsync(1));
    }

    [E2eFact(DisplayName = "On a phone the slider stops at its last card")]
    public async Task OnAPhoneTheSliderStopsAtItsLastCard()
    {
        var home = await OpenSliderAsync();

        for (var press = 1; press <= 3; press++)
        {
            await home.PressNextAsync();
            Assert.Equal(new[] { press }, await home.CardsInViewAsync(press));
        }

        Assert.Equal(new[] { 3 }, await home.CardsInViewAsync(3));
        Assert.True(await home.IsNextDisabledAsync(), "\"Next\" is disabled at the last card");
    }

    [E2eFact(DisplayName = "The homepage slider does not move by itself")]
    public async Task TheSliderDoesNotMoveByItself()
    {
        var home = await OpenSliderAsync();
        Assert.Equal(new[] { 0 }, await home.CardsInViewAsync(0));

        // The waiting is the observation here, not a synchronisation: ten seconds of nobody touching the slider.
        await Page.WaitForTimeoutAsync(10_000);

        Assert.True(await home.IsCardInViewAsync(0), "the first card is still in view");
        Assert.False(await home.IsCardInViewAsync(1), "the slider moved on to the second card by itself");
    }

    private async Task<HomePage> OpenSliderAsync()
    {
        var home = await HomePage.NavigateToAsync(Page);
        Assert.True(await home.ShowSliderAsync(), "the homepage shows the product slider");
        return home;
    }
}