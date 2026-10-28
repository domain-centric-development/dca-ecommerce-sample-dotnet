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
/// One event, two listeners: the wired shop with its real dispatcher and its default retry policy, one listener that
/// fails on every delivery and one that handles the event at once, and the event log page read afterwards.
/// </summary>
public sealed class BackofficeListenerAttemptsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BackofficeListenerAttemptsTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddScoped<IEventListener, GivingUpListener>();
            services.AddScoped<IEventListener, WillingListener>();
        }));
    }

    [Fact]
    public async Task AListenerThatKeepsFailingLeavesTheAttemptsOfTheOtherListenerAlone()
    {
        var @event = new SharedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await PublishAsync(@event);
        await SettledAsync(@event.EventId);

        var page = await EventLogAsync();

        Assert.Equal("Status: FAILED, attempts: 5", FirstPayloadLine(Entry(page, nameof(GivingUpListener))));
        Assert.Equal("Status: COMPLETED, attempts: 1", FirstPayloadLine(Entry(page, nameof(WillingListener))));
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

    /// <summary>The one entry of <see cref="SharedEvent"/> whose listener is <paramref name="listener"/>.</summary>
    private static string Entry(string page, string listener) =>
        Assert.Single(
            page.Split("data-test=\"event-log-item\"").Skip(1),
            entry => Value(entry, "event-type").EndsWith(nameof(SharedEvent), StringComparison.Ordinal)
                && Value(entry, "event-listener").EndsWith("+" + listener, StringComparison.Ordinal));

    private static string FirstPayloadLine(string entry)
    {
        var payload = Regex.Match(entry, @"class=""event-log__payload-content"">([^<]*)</pre>");
        Assert.True(payload.Success, "the entry carries its payload");
        return WebUtility.HtmlDecode(payload.Groups[1].Value).Split('\n')[0].TrimEnd('\r');
    }

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

    public sealed record SharedEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    /// <summary>Fails on every delivery.</summary>
    public sealed class GivingUpListener : TransactionalEventListener<SharedEvent>
    {
        protected override Task OnAsync(SharedEvent @event, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("breakdown");
    }

    /// <summary>Handles every delivery.</summary>
    public sealed class WillingListener : TransactionalEventListener<SharedEvent>
    {
        protected override Task OnAsync(SharedEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}