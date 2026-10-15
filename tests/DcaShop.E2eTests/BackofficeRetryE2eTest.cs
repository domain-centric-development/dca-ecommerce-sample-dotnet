using DcaShop.E2eTests.Pages;
using DcaShop.SharedKernel.Infrastructure.Events;

using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.E2eTests;

/// <summary>
/// The operator's retry of a delivery the shop gave up on. Nothing reachable from outside makes a delivery fail, so the
/// test starts a shop of its own beside the suite's, with a listener that fails on every delivery.
/// </summary>
public sealed class BackofficeRetryE2eTest : BaseE2eTest
{
    public BackofficeRetryE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [OwnShopFact(DisplayName = "Failed entry offers a retry")]
    public async Task AnEntryWhoseDeliveryUsedUpItsAttemptsOffersTheRetryOfItsListener()
    {
        using var shop = SideShop.Start(services => services.AddScoped<IEventListener, BrokenListener>());
        await PublishUntilFailedAsync(shop);

        var login = await BackofficeLoginPage.NavigateToAsync(Page, shop.BaseUrl);
        await login.FillCredentialsAsync("admin", "admin");
        var events = await login.SubmitAsync();

        Assert.Equal("Retry failed listener", await events.RetryButtonOfAsync(nameof(UndeliverableEvent)));
    }

    [OwnShopFact(DisplayName = "Retry completes a failed delivery")]
    public async Task RetryingADeliveryWhoseListenerHandlesTheEventAgainCompletesIt()
    {
        using var shop = SideShop.Start(services => services
            .AddSingleton<Repair>()
            .AddScoped<IEventListener, RepairableListener>());
        var @event = new RecoverableEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        using (var scope = shop.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(@event);
        }

        await QuietAsync(shop);
        Assert.Equal(PublicationStatus.Failed, shop.Services.GetRequiredService<IIntegrationEventOutbox>().All().Single(p => p.Id == @event.EventId).Status);
        shop.Services.GetRequiredService<Repair>().Done = true;

        var login = await BackofficeLoginPage.NavigateToAsync(Page, shop.BaseUrl);
        await login.FillCredentialsAsync("admin", "admin");
        var events = await login.SubmitAsync();
        var completed = await events.CompletedCountAsync();
        var incomplete = await events.IncompleteCountAsync();

        var next = await events.PressRetryOfAsync(nameof(RecoverableEvent));

        Assert.Equal("/backoffice/events", next);
        await QuietAsync(shop);
        var log = await BackofficeEventsPage.NavigateToAsync(Page, shop.BaseUrl);
        Assert.Equal("Completed", await log.BadgeOfAsync(nameof(RecoverableEvent)));
        Assert.Matches(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$", await log.CompletionDateOfAsync(nameof(RecoverableEvent)));
        Assert.Equal(completed + 1, await log.CompletedCountAsync());
        Assert.Equal(incomplete - 1, await log.IncompleteCountAsync());
    }

    [OwnShopFact(DisplayName = "A delivery under way offers no retry")]
    public async Task AnEntryWhoseDeliveryIsStillUnderWayOffersNoRetry()
    {
        using var shop = SideShop.Start(services => services
            .AddSingleton<Linger>()
            .AddScoped<IEventListener, LingeringListener>());
        using (var scope = shop.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>()
                .PublishAsync(new LingeringEvent(Guid.NewGuid(), DateTimeOffset.UtcNow));
        }

        await shop.Services.GetRequiredService<Linger>().Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var login = await BackofficeLoginPage.NavigateToAsync(Page, shop.BaseUrl);
        await login.FillCredentialsAsync("admin", "admin");
        var events = await login.SubmitAsync();

        Assert.Equal("Incomplete", await events.BadgeOfAsync(nameof(LingeringEvent)));
        Assert.False(await events.OffersRetryOfAsync(nameof(LingeringEvent)), "the entry under way offers a retry");
    }

    /// <summary>Waits until the shop has stopped working on every delivery, whichever state each ended in — or gives up waiting.</summary>
    private static async Task QuietAsync(SideShop shop)
    {
        var outbox = shop.Services.GetRequiredService<IIntegrationEventOutbox>();
        bool Quiet() => outbox.All().All(p => p.Status is PublicationStatus.Completed or PublicationStatus.Failed);
        for (var i = 0; i < 200 && !Quiet(); i++)
        {
            await Task.Delay(50);
        }

        Assert.True(Quiet(), "the shop did not stop delivering");
    }

    /// <summary>Publishes an event only the broken listener receives and waits until the shop has given it up.</summary>
    private static async Task PublishUntilFailedAsync(SideShop shop)
    {
        var @event = new UndeliverableEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        using (var scope = shop.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(@event);
        }

        var outbox = shop.Services.GetRequiredService<IIntegrationEventOutbox>();
        for (var i = 0; i < 200 && outbox.All().Single(p => p.Id == @event.EventId).Status != PublicationStatus.Failed; i++)
        {
            await Task.Delay(50);
        }

        Assert.Equal(PublicationStatus.Failed, outbox.All().Single(p => p.Id == @event.EventId).Status);
    }

    public sealed record UndeliverableEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    /// <summary>Fails on every delivery, so the shop uses up its attempts and gives up.</summary>
    public sealed class BrokenListener : EventListener<UndeliverableEvent>
    {
        protected override Task OnAsync(UndeliverableEvent @event, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("listener unavailable");
    }

    public sealed record RecoverableEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    /// <summary>Whether the repairable listener has been repaired.</summary>
    public sealed class Repair
    {
        public volatile bool Done;
    }

    /// <summary>Fails on every delivery until it is repaired.</summary>
    public sealed class RepairableListener : EventListener<RecoverableEvent>
    {
        private readonly Repair _repair;

        public RepairableListener(Repair repair) => _repair = repair;

        protected override Task OnAsync(RecoverableEvent @event, CancellationToken cancellationToken) =>
            _repair.Done ? Task.CompletedTask : throw new InvalidOperationException("listener unavailable");
    }

    public sealed record LingeringEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    /// <summary>Signals once the lingering listener has begun a delivery.</summary>
    public sealed class Linger
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Begins every delivery and never finishes it while the shop runs, so the delivery stays under way.</summary>
    public sealed class LingeringListener : EventListener<LingeringEvent>
    {
        private readonly Linger _linger;

        public LingeringListener(Linger linger) => _linger = linger;

        protected override async Task OnAsync(LingeringEvent @event, CancellationToken cancellationToken)
        {
            _linger.Entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
}