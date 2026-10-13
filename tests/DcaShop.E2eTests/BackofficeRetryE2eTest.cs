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
}