using System.Net;
using System.Text.RegularExpressions;

using DcaShop.SharedKernel.Infrastructure.Events;

using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// What the operator sees of the shop's own retries: the wired shop with its real dispatcher and its default retry
/// policy, a listener that fails once and one that fails on every delivery, and the event log page read afterwards.
/// </summary>
public sealed class BackofficeRedeliveryTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string DateForm = @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$";

    private readonly WebApplicationFactory<Program> _factory;

    public BackofficeRedeliveryTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<Deliveries>();
            services.AddScoped<IEventListener, HiccupListener>();
            services.AddScoped<IEventListener, BreakdownListener>();
        }));
    }

    [Fact]
    public async Task APassingFailureHealsWithoutTheOperator()
    {
        var @event = new HiccupEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await PublishAsync(@event);
        await SettledAsync(@event.EventId);

        var entry = Entry(await EventLogAsync(), nameof(HiccupEvent));

        Assert.Equal("Completed", Value(entry, "event-status"));
        Assert.Matches(DateForm, Value(entry, "event-completion-date"));
        Assert.Equal("Status: COMPLETED, attempts: 2", FirstPayloadLine(entry));
    }

    [Fact]
    public async Task AnExhaustedDeliveryStaysIncomplete()
    {
        var @event = new BreakdownEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await PublishAsync(@event);
        await SettledAsync(@event.EventId);

        var entry = Entry(await EventLogAsync(), nameof(BreakdownEvent));

        Assert.Equal("Incomplete", Value(entry, "event-status"));
        Assert.DoesNotContain("data-test=\"event-completion-date\"", entry, StringComparison.Ordinal);
        Assert.Equal("Status: FAILED, attempts: 5", FirstPayloadLine(entry));
    }

    [Fact]
    public async Task AnExhaustedDeliveryIsCountedAsIncomplete()
    {
        var @event = new BreakdownEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await PublishAsync(@event);
        await SettledAsync(@event.EventId);

        var page = await EventLogAsync();

        var badges = Entries(page).Select(entry => Value(entry, "event-status")).ToList();
        Assert.Equal("Incomplete", Value(Entry(page, nameof(BreakdownEvent)), "event-status"));
        Assert.Equal(badges.Count(badge => badge == "Incomplete"), Number(page, "event-log-incomplete"));
        Assert.Equal(badges.Count(badge => badge == "Completed"), Number(page, "event-log-completed"));
    }

    private async Task PublishAsync(IIntegrationEvent @event)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(@event);
    }

    /// <summary>Waits until the shop has stopped working on the delivery — or gives up waiting; the page decides.</summary>
    private async Task SettledAsync(Guid eventId)
    {
        var outbox = _factory.Services.GetRequiredService<IEventOutbox>();
        for (var i = 0; i < 200; i++)
        {
            var status = outbox.All().Single(p => p.Id == eventId).Status;
            if (status is PublicationStatus.Completed or PublicationStatus.Failed)
            {
                return;
            }

            await Task.Delay(50);
        }
    }

    /// <summary>The event log page, signed in as the operator.</summary>
    private async Task<string> EventLogAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var form = Element(await client.GetStringAsync("/backoffice/login"), "backoffice-login-form", "</form>");
        var hidden = Regex.Matches(form, @"<input\b[^>]*type=""hidden""[^>]*>")
            .Select(input => new KeyValuePair<string, string>(
                Regex.Match(input.Value, @"name=""([^""]+)""").Groups[1].Value,
                WebUtility.HtmlDecode(Regex.Match(input.Value, @"value=""([^""]*)""").Groups[1].Value)));
        var login = await client.PostAsync(
            "/backoffice/login",
            new FormUrlEncodedContent(hidden.Concat(
                [new KeyValuePair<string, string>("username", "admin"), new KeyValuePair<string, string>("password", "admin")])));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return await client.GetStringAsync("/backoffice/events");
    }

    /// <summary>The markup of each listed entry, from its start to the next one's.</summary>
    private static IReadOnlyList<string> Entries(string page) =>
        page.Split("data-test=\"event-log-item\"").Skip(1).ToList();

    /// <summary>The one entry whose type label names <paramref name="eventType"/>.</summary>
    private static string Entry(string page, string eventType) =>
        Assert.Single(Entries(page), entry => Value(entry, "event-type").EndsWith(eventType, StringComparison.Ordinal));

    private static string FirstPayloadLine(string entry)
    {
        var payload = Regex.Match(entry, @"class=""event-log__payload-content"">([^<]*)</pre>");
        Assert.True(payload.Success, "the entry carries its payload");
        return WebUtility.HtmlDecode(payload.Groups[1].Value).Split('\n')[0].TrimEnd('\r');
    }

    private static int Number(string page, string dataTest) =>
        int.Parse(Value(page, dataTest), System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The text of the first element carrying <paramref name="dataTest"/>.</summary>
    private static string Value(string markup, string dataTest)
    {
        var match = Regex.Match(markup, $@"data-test=""{Regex.Escape(dataTest)}"">([^<]*)<");
        Assert.True(match.Success, $"the markup has a {dataTest}");
        return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
    }

    /// <summary>The markup from the element carrying <paramref name="dataTest"/> to its closing tag.</summary>
    private static string Element(string page, string dataTest, string closingTag)
    {
        var start = page.IndexOf($"data-test=\"{dataTest}\"", StringComparison.Ordinal);
        Assert.True(start >= 0, $"the page has a {dataTest}");
        return page[start..].Split(closingTag)[0];
    }

    public sealed record HiccupEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    public sealed record BreakdownEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    public sealed class Deliveries
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, int> _counts = new();

        public int Next(Guid id) => _counts.AddOrUpdate(id, 1, (_, n) => n + 1);
    }

    /// <summary>Fails on the first delivery of an event and handles it from then on.</summary>
    public sealed class HiccupListener : TransactionalEventListener<HiccupEvent>
    {
        private readonly Deliveries _deliveries;

        public HiccupListener(Deliveries deliveries) => _deliveries = deliveries;

        protected override Task OnAsync(HiccupEvent @event, CancellationToken cancellationToken) =>
            _deliveries.Next(@event.EventId) == 1 ? throw new InvalidOperationException("hiccup") : Task.CompletedTask;
    }

    /// <summary>Fails on every delivery.</summary>
    public sealed class BreakdownListener : TransactionalEventListener<BreakdownEvent>
    {
        protected override Task OnAsync(BreakdownEvent @event, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("breakdown");
    }
}