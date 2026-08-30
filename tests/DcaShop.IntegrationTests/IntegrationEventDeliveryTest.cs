using DcaShop.Infrastructure.Events;
using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>The outbox is written in the use case's transaction and delivers at least once: transient listener failures are retried, permanent ones end up as Failed.</summary>
public sealed class IntegrationEventDeliveryTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationEventDeliveryTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(new IntegrationEventRetryPolicy(3, TimeSpan.FromMilliseconds(10)));
            services.AddSingleton<Attempts>();
            services.AddScoped<IEventListener, FlakyListener>();
            services.AddScoped<IEventListener, PoisonListener>();
        }));
    }

    [Fact]
    public async Task TransientFailureIsRetriedUntilDelivered()
    {
        var @event = new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);

        await PublishAsync(@event);

        var publication = await Eventually(() => Publication(@event.EventId), p => p.Status == PublicationStatus.Completed);
        Assert.Equal(1, publication.Attempts);   // one failed attempt recorded, second succeeded
        Assert.Equal(2, _factory.Services.GetRequiredService<Attempts>().Count(@event.EventId));
    }

    [Fact]
    public async Task PermanentFailureIsMarkedFailedAfterRetryPolicy()
    {
        var @event = new PoisonEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);

        await PublishAsync(@event);

        var publication = await Eventually(() => Publication(@event.EventId), p => p.Status == PublicationStatus.Failed);
        Assert.Equal(3, publication.Attempts);
        Assert.Equal("poison", publication.LastError);
    }

    [Fact]
    public async Task PublicationIsRegisteredInsideTheTransactionAndReleasedAfterCommit()
    {
        var @event = new QuietEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        using var scope = _factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        var boundary = scope.ServiceProvider.GetRequiredService<ITransactionBoundary>();
        IntegrationEventPublication? insideTransaction = null;

        await boundary.InTransactionAsync(async ct =>
        {
            await publisher.PublishAsync(@event, ct);
            insideTransaction = Publication(@event.EventId);   // outbox row exists together with the aggregate
            await Task.Delay(100, ct);                         // dispatcher must not see it yet
            Assert.Equal(PublicationStatus.Pending, Publication(@event.EventId).Status);
            Assert.Equal(0, Publication(@event.EventId).Attempts);
        });

        Assert.NotNull(insideTransaction);
        await Eventually(() => Publication(@event.EventId), p => p.Status == PublicationStatus.Completed);
    }

    [Fact]
    public async Task RolledBackTransactionLeavesNoPublicationBehind()
    {
        var @event = new QuietEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        using var scope = _factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        var boundary = scope.ServiceProvider.GetRequiredService<ITransactionBoundary>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.InTransactionAsync<int>(async ct =>
        {
            await publisher.PublishAsync(@event, ct);
            throw new InvalidOperationException("boom");
        }));

        Assert.DoesNotContain(Outbox.All(), p => p.Id == @event.EventId);
    }

    private async Task PublishAsync(IIntegrationEvent @event)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(@event);
    }

    private IIntegrationEventOutbox Outbox => _factory.Services.GetRequiredService<IIntegrationEventOutbox>();

    private IntegrationEventPublication Publication(Guid id) => Outbox.All().Single(p => p.Id == id);

    private static async Task<T> Eventually<T>(Func<T> read, Func<T, bool> condition)
    {
        T value = read();
        for (var i = 0; i < 100 && !condition(value); i++)
        {
            await Task.Delay(50);
            value = read();
        }

        Assert.True(condition(value), $"condition not met within 5 seconds; last state: {value}");
        return value;
    }

    public sealed record ProbeEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    public sealed record PoisonEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    /// <summary>No listener — delivery completes trivially.</summary>
    public sealed record QuietEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    public sealed class Attempts
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, int> _counts = new();

        public int Next(Guid id) => _counts.AddOrUpdate(id, 1, (_, n) => n + 1);

        public int Count(Guid id) => _counts.GetValueOrDefault(id);
    }

    /// <summary>Fails on the first delivery of each event, succeeds afterwards.</summary>
    public sealed class FlakyListener : EventListener<ProbeEvent>
    {
        private readonly Attempts _attempts;

        public FlakyListener(Attempts attempts)
        {
            _attempts = attempts;
        }

        protected override Task OnAsync(ProbeEvent @event, CancellationToken cancellationToken) =>
            _attempts.Next(@event.EventId) == 1 ? throw new InvalidOperationException("transient") : Task.CompletedTask;
    }

    public sealed class PoisonListener : EventListener<PoisonEvent>
    {
        protected override Task OnAsync(PoisonEvent @event, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("poison");
    }
}
