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
    private const string EventLogItem = "event-log-item";
    private const string EventType = "event-type";
    private const string EventStatus = "event-status";
    private const string EventListener = "event-listener";
    private const string EventPayload = "event-payload";
    private const string EventRetry = "event-retry";
    private const string EventCompletionDate = "event-completion-date";

    private BackofficeEventsPage(IPage page) : base(page)
    {
    }

    public static async Task<BackofficeEventsPage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + "/backoffice/events");
        return await OpenAsync(page);
    }

    /// <summary>Waits for the log on whichever shop the browser is on — the suite's own or one beside it.</summary>
    public static async Task<BackofficeEventsPage> OpenAsync(IPage page)
    {
        var events = new BackofficeEventsPage(page);
        await page.WaitForLoadedUrlAsync("**" + UrlPattern);
        return events;
    }

    public Task<bool> IsOnPageAsync() => ExistsAsync(EventLog);

    public Task<bool> HasSummaryAsync() => ExistsAsync(EventLogSummary);

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
    /// The lines of the "Event Payload" of the first entry of the given type that carries the badge "Completed", as the
    /// browser shows them once the payload is expanded.
    /// </summary>
    public async Task<IReadOnlyList<string>> CompletedPayloadLinesOfAsync(string eventType)
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

    /// <summary>
    /// The lines of the "Event Payload" of the entry of the given type whose listener is the given one, as the browser
    /// shows them once the payload is expanded.
    /// </summary>
    public async Task<IReadOnlyList<string>> PayloadLinesOfAsync(string eventType, string listener)
    {
        var entry = Page.Locator($"[data-test='{EventLogItem}']")
            .Filter(new() { Has = Page.Locator($"[data-test='{EventType}']", new() { HasTextString = eventType }) })
            .Filter(new() { Has = Page.Locator($"[data-test='{EventListener}']", new() { HasTextString = listener }) });
        await Assertions.Expect(entry).ToBeVisibleAsync();

        var payload = entry.Locator($"[data-test='{EventPayload}']");
        await payload.Locator("summary").ClickAsync();
        var content = payload.Locator("pre");
        await Assertions.Expect(content).ToBeVisibleAsync();

        return (await content.InnerTextAsync()).Split('\n').Select(line => line.TrimEnd('\r')).ToList();
    }

    /// <summary>
    /// The label of the retry button on the first entry of the given type that carries the badge "Incomplete", once it is
    /// visible.
    /// </summary>
    public async Task<string> RetryButtonOfAsync(string eventType)
    {
        var entry = Page.Locator($"[data-test='{EventLogItem}']")
            .Filter(new() { Has = Page.Locator($"[data-test='{EventType}']", new() { HasTextString = eventType }) })
            .Filter(new() { Has = Page.Locator($"[data-test='{EventStatus}']", new() { HasTextString = "Incomplete" }) })
            .First;
        await Assertions.Expect(entry).ToBeVisibleAsync();

        var retry = entry.Locator($"[data-test='{EventRetry}']");
        await Assertions.Expect(retry).ToBeVisibleAsync();
        return (await retry.InnerTextAsync()).Trim();
    }

    /// <summary>
    /// Presses the retry on the first entry of the given type that carries the badge "Incomplete" and answers the path
    /// of the page the shop opens next.
    /// </summary>
    public async Task<string> PressRetryOfAsync(string eventType)
    {
        var retry = IncompleteEntryOf(eventType).Locator($"[data-test='{EventRetry}']");
        await Assertions.Expect(retry).ToBeVisibleAsync();
        await Page.RunAndWaitForNextPageAsync(() => retry.ClickAsync());
        return new Uri(Page.Url).AbsolutePath;
    }

    /// <summary>The badge of the first entry of the given type, as the markup words it (the stylesheet capitalises it).</summary>
    public async Task<string> BadgeOfAsync(string eventType)
    {
        var badge = EntryOf(eventType).Locator($"[data-test='{EventStatus}']");
        await Assertions.Expect(badge).ToBeVisibleAsync();
        return (await badge.TextContentAsync() ?? string.Empty).Trim();
    }

    /// <summary>Whether the first entry of the given type offers a retry button, once the entry is visible.</summary>
    public async Task<bool> OffersRetryOfAsync(string eventType)
    {
        var entry = EntryOf(eventType);
        await Assertions.Expect(entry).ToBeVisibleAsync();
        return await entry.Locator($"[data-test='{EventRetry}']").CountAsync() > 0;
    }

    /// <summary>The "Completed" date of the first entry of the given type, once it shows one.</summary>
    public async Task<string> CompletionDateOfAsync(string eventType)
    {
        var date = EntryOf(eventType).Locator($"[data-test='{EventCompletionDate}']");
        await Assertions.Expect(date).ToBeVisibleAsync();
        return (await date.InnerTextAsync()).Trim();
    }

    /// <summary>Opens the log of the shop at <paramref name="shopUrl"/>, a shop other than the suite's own.</summary>
    public static async Task<BackofficeEventsPage> NavigateToAsync(IPage page, string shopUrl)
    {
        await page.GotoAsync(shopUrl + "/backoffice/events");
        return await OpenAsync(page);
    }

    private ILocator EntryOf(string eventType) =>
        Page.Locator($"[data-test='{EventLogItem}']")
            .Filter(new() { Has = Page.Locator($"[data-test='{EventType}']", new() { HasTextString = eventType }) })
            .First;

    private ILocator IncompleteEntryOf(string eventType) =>
        Page.Locator($"[data-test='{EventLogItem}']")
            .Filter(new() { Has = Page.Locator($"[data-test='{EventType}']", new() { HasTextString = eventType }) })
            .Filter(new() { Has = Page.Locator($"[data-test='{EventStatus}']", new() { HasTextString = "Incomplete" }) })
            .First;

    private async Task<int> NumberAsync(string dataTest) =>
        int.Parse(await TextAsync(dataTest), CultureInfo.InvariantCulture);

    private async Task<string> TextAsync(string dataTest)
    {
        await WaitForAsync(dataTest);
        return (await Page.Locator($"[data-test='{dataTest}']").First.TextContentAsync() ?? string.Empty).Trim();
    }
}