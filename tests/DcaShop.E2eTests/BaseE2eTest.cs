using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>Browser per test class, fresh context + page per test — the shape of the Java sample's <c>BaseE2ETest</c>.</summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    public static readonly string BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5080";
    private static readonly string BrowserType = Environment.GetEnvironmentVariable("E2E_BROWSER") ?? "chromium";
    private static readonly bool Headless = !string.Equals(Environment.GetEnvironmentVariable("E2E_HEADLESS"), "false", StringComparison.OrdinalIgnoreCase);

    private IPlaywright? _playwright;

    public IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser fixture not initialised — is E2E_BASE_URL set?");

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

    public async Task InitializeAsync()
    {
        _context = await _browser.Browser.NewContextAsync();
        Page = await _context.NewPageAsync();
    }

    public Task DisposeAsync() => _context?.CloseAsync() ?? Task.CompletedTask;

    protected Task NavigateToAsync(string path) => Page.GotoAsync(BaseUrl + path);

    /// <summary>
    /// Drops every cookie of this browser, which is how a test becomes a different visitor: no identity, no
    /// session, no cart.
    /// </summary>
    protected Task ClearCookiesAsync() => _context!.ClearCookiesAsync();

    protected Task WaitForUrlAsync(string pattern) => Page.WaitForURLAsync(BaseUrl + pattern);

    protected string CurrentPath => Page.Url.Replace(BaseUrl, string.Empty, StringComparison.Ordinal);
}
