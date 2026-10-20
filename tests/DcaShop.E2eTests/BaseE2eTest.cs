using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>Browser per test class, fresh context + page per test — the shape of the Java sample's <c>BaseE2ETest</c>.</summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    public static string BaseUrl => ShopUnderTest.BaseUrl;
    private static readonly string BrowserType = Environment.GetEnvironmentVariable("E2E_BROWSER") ?? "chromium";
    private static readonly bool Headless = !string.Equals(Environment.GetEnvironmentVariable("E2E_HEADLESS"), "false", StringComparison.OrdinalIgnoreCase);

    private IPlaywright? _playwright;

    public IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser fixture not initialised");

    private IBrowser? _browser;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        var options = new BrowserTypeLaunchOptions { Headless = Headless };
        _browser = BrowserType.ToLowerInvariant() switch
        {
            "firefox" => await _playwright.Firefox.LaunchAsync(options),
            "webkit" => await _playwright.Webkit.LaunchAsync(options),
            _ => await _playwright.Chromium.LaunchAsync(options),
        };
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }
        _playwright?.Dispose();
    }
}

public abstract class BaseE2eTest : IClassFixture<BrowserFixture>, IAsyncLifetime
{
    private readonly BrowserFixture _browser;
    private IBrowserContext? _context;

    protected BaseE2eTest(BrowserFixture browser)
    {
        _browser = browser;
    }

    protected IPage Page { get; private set; } = null!;

    protected static string BaseUrl => BrowserFixture.BaseUrl;

    /// <summary>Options for the browser context of each test; a suite that needs a phone viewport overrides this.</summary>
    protected virtual BrowserNewContextOptions? ContextOptions => null;

    public async Task InitializeAsync()
    {
        _context = await _browser.Browser.NewContextAsync(ContextOptions);
        Page = await _context.NewPageAsync();
    }

    public Task DisposeAsync() => _context?.CloseAsync() ?? Task.CompletedTask;

    protected Task NavigateToAsync(string path) => Page.GotoAsync(BaseUrl + path);

    /// <summary>
    /// Drops every cookie of this browser, which is how a test becomes a different visitor: no identity, no
    /// session, no cart.
    /// </summary>
    protected Task ClearCookiesAsync() => _context!.ClearCookiesAsync();

    /// <summary>A new tab in this browser — what a visitor opens after closing the shop and coming back.</summary>
    protected Task<IPage> NewPageInThisBrowserAsync() => _context!.NewPageAsync();

    /// <summary>A second browser with storage and cookies of its own; the caller disposes it.</summary>
    protected Task<IBrowserContext> NewBrowserAsync() => _browser.Browser.NewContextAsync(ContextOptions);

    protected Task WaitForUrlAsync(string pattern) => Page.WaitForLoadedUrlAsync(BaseUrl + pattern);

    /// <summary>
    /// Proceeds in <paramref name="page"/> from the cart through buyer, delivery and payment of the shop at
    /// <paramref name="shopUrl"/>, and answers the review it lands on.
    /// </summary>
    protected static async Task<ReviewPage> GuestFromTheCartToTheReviewAsync(IPage page, string shopUrl)
    {
        await (await CartPage.NavigateToAsync(page, shopUrl)).PressProceedToCheckoutAsync();
        var buyer = await BuyerInfoPage.OpenAsync(page, shopUrl);
        await buyer.FillBuyerInfoAsync("jane@example.com", "Jane", "Doe", "+49 30 1234567");
        await buyer.PressContinueToDeliveryAsync();
        var delivery = await DeliveryPage.OpenAsync(page, shopUrl);
        await delivery.FillAddressAsync("Hauptstrasse 1", "Berlin", "10115", "Germany", string.Empty);
        await delivery.ChooseShippingMethodAsync("Standard Shipping");
        await delivery.SubmitAndShowNextPathAsync();
        var payment = await PaymentPage.OpenAsync(page, shopUrl);
        await payment.ChooseTheOfferedPaymentProviderAsync();
        await payment.SubmitAndShowNextPathAsync();
        return await ReviewPage.OpenAsync(page, shopUrl);
    }

    protected string CurrentPath => Page.Url.Replace(BaseUrl, string.Empty, StringComparison.Ordinal);
}