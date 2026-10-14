using DcaShop.E2eTests.Pages;
using DcaShop.SharedKernel.Infrastructure.Events;

using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.E2eTests;

/// <summary>
/// The attempts each listener's entry counts. Nothing reachable from outside makes a delivery fail, so the test starts a
/// shop of its own beside the suite's, with two listeners of one event that fail a set number of times.
/// </summary>
public sealed class BackofficeAttemptsE2eTest : BaseE2eTest
{
    public BackofficeAttemptsE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [OwnShopFact(DisplayName = "Each listener counts its own attempts")]
    public async Task EachListenersEntryCountsTheTriesOfItsOwnDelivery()
    {
        using var shop = SideShop.Start(services => services
            .AddSingleton<Deliveries>()
            .AddScoped<IEventListener, TwiceFailingListener>()
            .AddScoped<IEventListener, OnceFailingListener>());
        await PublishUntilSettledAsync(shop);

        var login = await BackofficeLoginPage.NavigateToAsync(Page, shop.BaseUrl);
        await login.FillCredentialsAsync("admin", "admin");
        var events = await login.SubmitAsync();

        var first = await events.PayloadLinesOfAsync(nameof(BumpyEvent), "+" + nameof(TwiceFailingListener));
        var second = await events.PayloadLinesOfAsync(nameof(BumpyEvent), "+" + nameof(OnceFailingListener));
        Assert.Equal("Status: COMPLETED, attempts: 3", first[0]);
        Assert.Equal("Status: COMPLETED, attempts: 2", second[0]);
    }

    /// <summary>Publishes the event both listeners receive and waits until the shop has stopped delivering it.</summary>
    private static async Task PublishUntilSettledAsync(SideShop shop)
    {
        var @event = new BumpyEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        using (var scope = shop.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(@event);
        }

        var outbox = shop.Services.GetRequiredService<IIntegrationEventOutbox>();
        bool Settled() => outbox.All().Single(p => p.Id == @event.EventId).Status is PublicationStatus.Completed or PublicationStatus.Failed;
        for (var i = 0; i < 200 && !Settled(); i++)
        {
            await Task.Delay(50);
        }

        Assert.True(Settled(), "the shop did not stop delivering");
    }

    public sealed record BumpyEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    /// <summary>How often each listener has received each event.</summary>
    public sealed class Deliveries
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<(string, Guid), int> _counts = new();

        public int Next(string listener, Guid id) => _counts.AddOrUpdate((listener, id), 1, (_, n) => n + 1);
    }

    /// <summary>Fails on the first two deliveries of an event and handles it from then on.</summary>
    public sealed class TwiceFailingListener : EventListener<BumpyEvent>
    {
        private readonly Deliveries _deliveries;

        public TwiceFailingListener(Deliveries deliveries) => _deliveries = deliveries;

        protected override Task OnAsync(BumpyEvent @event, CancellationToken cancellationToken) =>
            _deliveries.Next(nameof(TwiceFailingListener), @event.EventId) <= 2
                ? throw new InvalidOperationException("listener unavailable")
                : Task.CompletedTask;
    }

    /// <summary>Fails on the first delivery of an event and handles it from then on.</summary>
    public sealed class OnceFailingListener : EventListener<BumpyEvent>
    {
        private readonly Deliveries _deliveries;

        public OnceFailingListener(Deliveries deliveries) => _deliveries = deliveries;

        protected override Task OnAsync(BumpyEvent @event, CancellationToken cancellationToken) =>
            _deliveries.Next(nameof(OnceFailingListener), @event.EventId) == 1
                ? throw new InvalidOperationException("listener unavailable")
                : Task.CompletedTask;
    }
}