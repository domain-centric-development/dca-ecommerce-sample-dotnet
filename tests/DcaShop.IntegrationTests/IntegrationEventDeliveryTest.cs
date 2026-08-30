using DcaShop.Infrastructure.Events;
using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>The outbox delivers at least once: transient listener failures are retried, permanent ones end up as Failed.</summary>
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

    private async Task PublishAsync(IIntegrationEvent @event)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>().PublishAsync(@event);
    }

    private IntegrationEventPublication Publication(Guid id) =>
        _factory.Services.GetRequiredService<IIntegrationEventOutbox>().All().Single(p => p.Id == id);

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
