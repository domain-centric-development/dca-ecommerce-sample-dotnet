using System.Net;
using System.Text.RegularExpressions;

using DcaShop.Backoffice.Application.GetEventPublications;
using DcaShop.Backoffice.Application.Shared;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The event log page's translation of what the log holds: the badge, listener and dates of each entry and the
/// summary above them. The input port is replaced by one that answers a completed delivery and one in each state
/// that has not completed yet — the states the seeded shop passes through too quickly for a browser to see.
/// </summary>
public sealed class BackofficeEventLogPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string DateForm = @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$";

    private static readonly DateTimeOffset Published = new(2026, 10, 1, 9, 30, 0, TimeSpan.Zero);

    private static readonly EventPublicationEntry Delivered = new(
        Guid.NewGuid(), "DcaShop.Product.Events.ProductCreatedEvent", "{}", "PriceInitializationEventConsumer",
        Published, Published.AddSeconds(2), "Completed");

    private static readonly EventPublicationEntry Staged = new(
        Guid.NewGuid(), "DcaShop.Product.Events.ProductCreatedEvent", "{}", "integration-event-dispatcher",
        Published.AddMinutes(1), null, "Staged");

    private static readonly EventPublicationEntry Pending = new(
        Guid.NewGuid(), "DcaShop.Product.Events.ProductCreatedEvent", "{}", "StockInitializationEventConsumer",
        Published.AddMinutes(2), null, "Pending");

    private static readonly EventPublicationEntry Failed = new(
        Guid.NewGuid(), "DcaShop.Checkout.Events.CheckoutConfirmedEvent", "{}", "CartCompletionEventConsumer",
        Published.AddMinutes(3), null, "Failed");

    private readonly WebApplicationFactory<Program> _factory;

    public BackofficeEventLogPageTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EveryEntryShowsItsBadgeListenerAndDates()
    {
        var page = await EventLogAsync(Delivered, Staged, Pending, Failed);

        var entries = Entries(page);
        Assert.Equal(4, entries.Count);
        Assert.Equal(["Completed", "Incomplete", "Incomplete", "Incomplete"], entries.Select(Badge));
        Assert.All(entries, entry =>
        {
            Assert.Equal(["Listener", "Published"], DetailLabels(entry).Take(2));
            Assert.False(string.IsNullOrWhiteSpace(Value(entry, "event-listener")));
            Assert.Matches(DateForm, Value(entry, "event-publication-date"));
        });
        Assert.All(entries.Where(entry => Badge(entry) == "Completed"), entry =>
        {
            Assert.Contains("Completed", DetailLabels(entry));
            Assert.Matches(DateForm, Value(entry, "event-completion-date"));
        });
    }

    [Fact]
    public async Task TheSummaryCountsTheEntriesByTheirBadge()
    {
        var page = await EventLogAsync(Delivered, Staged, Pending, Failed);

        var badges = Entries(page).Select(Badge).ToList();
        var total = Number(page, "event-log-total");
        var completed = Number(page, "event-log-completed");
        var incomplete = Number(page, "event-log-incomplete");
        Assert.Equal(badges.Count, total);
        Assert.Equal(badges.Count(badge => badge == "Completed"), completed);
        Assert.Equal(badges.Count(badge => badge == "Incomplete"), incomplete);
        Assert.Equal(total, completed + incomplete);
    }

    [Fact]
    public async Task AFailedEntryKeepsItsRetryButton()
    {
        var page = await EventLogAsync(Delivered, Staged, Pending, Failed);

        var retries = Entries(page).Where(entry => entry.Contains("/replay\"", StringComparison.Ordinal)).ToList();
        Assert.Single(retries);
        Assert.Contains($"action=\"/backoffice/events/{Failed.Id}/replay\"", retries[0], StringComparison.Ordinal);
    }

    /// <summary>The event log page, signed in as the operator, over a log that holds exactly <paramref name="entries"/>.</summary>
    private async Task<string> EventLogAsync(params EventPublicationEntry[] entries)
    {
        var shop = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<IGetEventPublicationsInputPort>(new AnsweredLog(entries))));
        var client = shop.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var login = await LogInAsync(client, "admin", "admin");
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/backoffice/events", login.Headers.Location!.ToString());
        return await client.GetStringAsync("/backoffice/events");
    }

    /// <summary>Fills in the login form as rendered, its antiforgery field included, and submits it.</summary>
    private static async Task<HttpResponseMessage> LogInAsync(HttpClient client, string username, string password)
    {
        var form = Element(await client.GetStringAsync("/backoffice/login"), "backoffice-login-form", "</form>");
        var hidden = Regex.Matches(form, @"<input\b[^>]*type=""hidden""[^>]*>")
            .Select(input => new KeyValuePair<string, string>(
                Regex.Match(input.Value, @"name=""([^""]+)""").Groups[1].Value,
                WebUtility.HtmlDecode(Regex.Match(input.Value, @"value=""([^""]*)""").Groups[1].Value)));

        return await client.PostAsync(
            "/backoffice/login",
            new FormUrlEncodedContent(hidden.Concat(
                [new KeyValuePair<string, string>("username", username), new KeyValuePair<string, string>("password", password)])));
    }

    /// <summary>The markup of each listed entry, from its start to the next one's.</summary>
    private static IReadOnlyList<string> Entries(string page) =>
        page.Split("data-test=\"event-log-item\"").Skip(1).ToList();

    private static string Badge(string entry) => Value(entry, "event-status");

    private static IReadOnlyList<string> DetailLabels(string entry) =>
        Regex.Matches(entry, @"class=""event-log__detail-label"">([^<]*)<").Select(m => Rendered(m.Groups[1].Value)).ToList();

    private static int Number(string page, string dataTest) => int.Parse(Value(page, dataTest), System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The text of the first element carrying <paramref name="dataTest"/>.</summary>
    private static string Value(string markup, string dataTest)
    {
        var match = Regex.Match(markup, $@"data-test=""{Regex.Escape(dataTest)}"">([^<]*)<");
        Assert.True(match.Success, $"the markup has a {dataTest}");
        return Rendered(match.Groups[1].Value);
    }

    /// <summary>The markup from the element carrying <paramref name="dataTest"/> to its closing tag.</summary>
    private static string Element(string page, string dataTest, string closingTag)
    {
        var start = page.IndexOf($"data-test=\"{dataTest}\"", StringComparison.Ordinal);
        Assert.True(start >= 0, $"the page has a {dataTest}");
        return page[start..].Split(closingTag)[0];
    }

    /// <summary>The text a reader sees: entities decoded, whitespace trimmed.</summary>
    private static string Rendered(string html) => WebUtility.HtmlDecode(html).Trim();

    /// <summary>The input port, answering the log the test arranged.</summary>
    private sealed class AnsweredLog : IGetEventPublicationsInputPort
    {
        private readonly IReadOnlyList<EventPublicationEntry> _entries;

        public AnsweredLog(IReadOnlyList<EventPublicationEntry> entries) => _entries = entries;

        public Task<GetEventPublicationsResult> ExecuteAsync(
            GetEventPublicationsQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(GetEventPublicationsResult.From(_entries));
    }
}