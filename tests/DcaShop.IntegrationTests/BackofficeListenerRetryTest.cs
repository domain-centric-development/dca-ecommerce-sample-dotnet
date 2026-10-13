using DcaShop.Backoffice.Application.GetEventPublications;
using DcaShop.Backoffice.Application.ReplayFailedPublication;
using DcaShop.SharedKernel.Infrastructure.Events;

using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The operator's retry of one listener's delivery, through the input ports of the wired shop: its real dispatcher, its
/// default retry policy, the outbox the log is read from — and an event with two listeners, one that handles it and one
/// that fails until it is repaired.
/// </summary>
public sealed class BackofficeListenerRetryTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BackofficeListenerRetryTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<Repair>();
            services.AddScoped<IEventListener, SteadyListener>();
            services.AddScoped<IEventListener, BrokenListener>();
        }));
    }

    [Fact]
    public async Task RetryingTheFailedListenerLeavesTheOtherListenersCompletionDateAsItWas()
    {
        var @event = new SplitEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await PublishAsync(@event);
        await SettledAsync(@event.EventId, PublicationStatus.Failed);
        var before = await EntriesOfAsync(@event.EventId);
        var steady = Assert.Single(before, entry => entry.ListenerId == typeof(SteadyListener).FullName);
        var broken = Assert.Single(before, entry => entry.ListenerId == typeof(BrokenListener).FullName);
        Assert.NotNull(steady.CompletionDate);
        Assert.Null(broken.CompletionDate);
        _factory.Services.GetRequiredService<Repair>().Done = true;

        using (var scope = _factory.Services.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<IReplayFailedPublicationInputPort>()
                .ExecuteAsync(new ReplayFailedPublicationCommand(broken.Id, broken.ListenerId));
            Assert.True(result.Found);
        }

        await SettledAsync(@event.EventId, PublicationStatus.Completed);
        var after = await EntriesOfAsync(@event.EventId);
        Assert.Equal(steady.CompletionDate, Assert.Single(after, entry => entry.ListenerId == steady.ListenerId).CompletionDate);
    }

    private async Task PublishAsync(IIntegrationEvent @event)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(@event);
    }

    /// <summary>The log's entries of one event, as the operator's query answers them.</summary>
    private async Task<IReadOnlyList<EventPublicationSummary>> EntriesOfAsync(Guid eventId)
    {
        using var scope = _factory.Services.CreateScope();
        var log = await scope.ServiceProvider.GetRequiredService<IGetEventPublicationsInputPort>()
            .ExecuteAsync(new GetEventPublicationsQuery());
        return log.Entries.Where(entry => entry.Id == eventId).ToList();
    }

    /// <summary>Waits until the shop has stopped working on the delivery in the given state — or gives up waiting.</summary>
    private async Task SettledAsync(Guid eventId, PublicationStatus status)
    {
        var outbox = _factory.Services.GetRequiredService<IIntegrationEventOutbox>();
        for (var i = 0; i < 200 && outbox.All().Single(p => p.Id == eventId).Status != status; i++)
        {
            await Task.Delay(50);
        }

        Assert.Equal(status, outbox.All().Single(p => p.Id == eventId).Status);
    }

    public sealed record SplitEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    /// <summary>Whether the broken listener has been repaired.</summary>
    public sealed class Repair
    {
        public volatile bool Done;
    }

    /// <summary>Handles every delivery.</summary>
    public sealed class SteadyListener : EventListener<SplitEvent>
    {
        protected override Task OnAsync(SplitEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>Fails on every delivery until it is repaired.</summary>
    public sealed class BrokenListener : EventListener<SplitEvent>
    {
        private readonly Repair _repair;

        public BrokenListener(Repair repair) => _repair = repair;

        protected override Task OnAsync(SplitEvent @event, CancellationToken cancellationToken) =>
            _repair.Done ? Task.CompletedTask : throw new InvalidOperationException("listener unavailable");
    }
}