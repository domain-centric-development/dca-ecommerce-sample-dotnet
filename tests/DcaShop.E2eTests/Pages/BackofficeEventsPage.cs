using System.Globalization;

using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>
/// The event publication log. Port of the Java sample's <c>BackofficeEventsPage</c>, same selectors — the page
/// shows the same three numbers, though this shop counts integration events rather than Modulith's per-listener
/// domain-event rows.
/// </summary>
public sealed class BackofficeEventsPage : BasePage
{
    private const string UrlPattern = "/backoffice/events**";
    private const string EventLog = "event-log";
    private const string EventLogSummary = "event-log-summary";
    private const string EventLogTotal = "event-log-total";
    private const string EventLogCompleted = "event-log-completed";
    private const string EventLogIncomplete = "event-log-incomplete";
    private const string EventLogList = "event-log-list";
    private const string EventLogItem = "event-log-item";
    private const string EventType = "event-type";
    private const string EventStatus = "event-status";
    private const string EventPayload = "event-payload";
    private const string EventLogRefresh = "event-log-refresh";
    private const string EventLogLogout = "event-log-logout";

    private BackofficeEventsPage(IPage page) : base(page)
    {
    }

    public static async Task<BackofficeEventsPage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + "/backoffice/events");
        return await OpenAsync(page);
    }

    public static async Task<BackofficeEventsPage> OpenAsync(IPage page)
    {
        var events = new BackofficeEventsPage(page);
        await events.WaitForUrlAsync(UrlPattern);
        return events;
    }

    public Task<bool> IsOnPageAsync() => ExistsAsync(EventLog);

    public Task<bool> HasSummaryAsync() => ExistsAsync(EventLogSummary);

    public Task<bool> HasEventListAsync() => ExistsAsync(EventLogList);

    public async Task<string> HeadingAsync() =>
        (await Page.Locator("main h1").InnerTextAsync()).Trim();

    public async Task<IReadOnlyList<string>> SummaryLabelsAsync()
    {
        await WaitForAsync(EventLogSummary);
        var labels = await Page.Locator($"[data-test='{EventLogSummary}'] .event-log__stat-label").AllInnerTextsAsync();
        return labels.Select(label => label.Trim()).ToList();
    }

    public Task<int> TotalEventsAsync() => NumberAsync(EventLogTotal);

    public Task<int> CompletedCountAsync() => NumberAsync(EventLogCompleted);

    public Task<int> IncompleteCountAsync() => NumberAsync(EventLogIncomplete);

    public Task<int> EventCountAsync() => Page.Locator($"[data-test='{EventLogItem}']").CountAsync();

    public async Task<IReadOnlyList<string>> EventTypesAsync() =>
        await Page.Locator($"[data-test='{EventType}']").AllTextContentsAsync();

    public Task<string> FirstEventTypeAsync() => TextAsync(EventType);

    public Task<string> FirstEventStatusAsync() => TextAsync(EventStatus);

    public async Task<BackofficeEventsPage> RefreshAsync()
    {
        await ClickAsync(EventLogRefresh);
        return await OpenAsync(Page);
    }

    public async Task<BackofficeLoginPage> LogoutAsync()
    {
        await ClickAsync(EventLogLogout);
        return await BackofficeLoginPage.OpenAsync(Page);
    }

    public async Task ExpandFirstEventPayloadAsync()
    {
        await WaitForAsync(EventPayload);
        await Page.Locator($"[data-test='{EventPayload}']").First.Locator("summary").ClickAsync();
    }

    /// <summary>
    /// Reads the payload content of the first entry as the browser shows it — the content alone, without the
    /// "Event Payload" summary, and only once it is visible.
    /// </summary>
    public async Task<string> FirstEventPayloadTextAsync()
    {
        var content = Page.Locator($"[data-test='{EventPayload}']").First.Locator("pre");
        await Assertions.Expect(content).ToBeVisibleAsync();
        return (await content.InnerTextAsync()).Trim();
    }

    /// <summary>
    /// Expands the "Event Payload" of the first entry of the given type that carries the badge "Completed" and reads the
    /// payload's lines as the browser shows them.
    /// </summary>
    public async Task<IReadOnlyList<string>> ExpandCompletedPayloadOfAsync(string eventType)
    {
        var entry = Page.Locator($"[data-test='{EventLogItem}']")
            .Filter(new() { Has = Page.Locator($"[data-test='{EventType}']", new() { HasTextString = eventType }) })
            .Filter(new() { Has = Page.Locator($"[data-test='{EventStatus}']", new() { HasTextString = "Completed" }) })
            .First;
        await Assertions.Expect(entry).ToBeVisibleAsync();

        var payload = entry.Locator($"[data-test='{EventPayload}']");
        await payload.Locator("summary").ClickAsync();
        var content = payload.Locator("pre");
        await Assertions.Expect(content).ToBeVisibleAsync();

        return (await content.InnerTextAsync()).Split('\n').Select(line => line.TrimEnd('\r')).ToList();
    }

    private async Task<int> NumberAsync(string dataTest) =>
        int.Parse(await TextAsync(dataTest), CultureInfo.InvariantCulture);

    private async Task<string> TextAsync(string dataTest)
    {
        await WaitForAsync(dataTest);
        return (await Page.Locator($"[data-test='{dataTest}']").First.TextContentAsync() ?? string.Empty).Trim();
    }
}