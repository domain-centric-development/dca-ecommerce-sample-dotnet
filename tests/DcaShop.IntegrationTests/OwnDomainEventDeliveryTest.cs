using DcaShop.Infrastructure.Events;
using DcaShop.SharedKernel.Adapter.Outgoing.Event;
using DcaShop.SharedKernel.Infrastructure.Events;
using DcaShop.SharedKernel.Infrastructure.Transactions;

using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The two listener kinds on a context's own domain event: the synchronous listener runs inside the publishing
/// transaction, the transactional listener only after the commit, from the outbox — and never after a rollback.
/// </summary>
public sealed class OwnDomainEventDeliveryTest
{
    [Fact]
    public async Task TheTransactionalListenerRunsAfterTheCommitAndTheSynchronousOneInsideIt()
    {
        var observed = new Observed();
        var boundary = new InMemoryTransactionBoundary();
        var outbox = new InMemoryEventOutbox(TimeProvider.System);
        using var services = Services(observed, boundary, outbox);
        using var dispatcher = Dispatcher(outbox, services);
        await dispatcher.StartAsync(default);

        using (var scope = services.CreateScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
            await boundary.InTransactionAsync(async ct =>
            {
                await publisher.PublishAsync(new ProbeHappened(Guid.NewGuid(), DateTimeOffset.UtcNow, "fact"), ct);
                Assert.Equal(1, observed.Synchronous);
                Assert.True(observed.SynchronousInTransaction);
                await Task.Delay(50, ct);
                Assert.Equal(0, observed.Reactions);
                return 0;
            });
        }

        await Until(() => observed.Reactions == 1);
        await dispatcher.StopAsync(default);
        Assert.False(observed.ReactionInTransaction);
        Assert.Equal("fact", observed.Payload);
        Assert.Equal(PublicationStatus.Completed, outbox.All().Single().Status);
    }

    [Fact]
    public async Task ARolledBackFactReachesNoTransactionalListener()
    {
        var observed = new Observed();
        var boundary = new InMemoryTransactionBoundary();
        var outbox = new InMemoryEventOutbox(TimeProvider.System);
        using var services = Services(observed, boundary, outbox);
        using var dispatcher = Dispatcher(outbox, services);
        await dispatcher.StartAsync(default);

        using (var scope = services.CreateScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.InTransactionAsync<int>(async ct =>
            {
                await publisher.PublishAsync(new ProbeHappened(Guid.NewGuid(), DateTimeOffset.UtcNow, "undone"), ct);
                throw new InvalidOperationException("rolled back");
            }));
        }

        await Task.Delay(100);
        await dispatcher.StopAsync(default);
        Assert.Equal(0, observed.Reactions);
        Assert.Empty(outbox.All());
    }

    [Fact]
    public async Task AnEventWithoutATransactionalListenerIsNotCaptured()
    {
        var observed = new Observed();
        var boundary = new InMemoryTransactionBoundary();
        var outbox = new InMemoryEventOutbox(TimeProvider.System);
        using var services = Services(observed, boundary, outbox);

        using var scope = services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
        await boundary.InTransactionAsync(async ct =>
        {
            await publisher.PublishAsync(new NobodyReacts(Guid.NewGuid(), DateTimeOffset.UtcNow), ct);
            return 0;
        });

        Assert.Empty(outbox.All());
    }

    private static ServiceProvider Services(Observed observed, InMemoryTransactionBoundary boundary, IEventOutbox outbox) =>
        new ServiceCollection()
            .AddSingleton(observed)
            .AddSingleton(boundary)
            .AddSingleton<ITransactionHooks>(boundary)
            .AddSingleton(outbox)
            .AddScoped<IEventDispatcher, InProcessEventDispatcher>()
            .AddScoped<IDomainEventPublisher, DomainEventPublisher>()
            .AddScoped<IEventListener, ProbeCounter>()
            .AddScoped<IEventListener, ProbeReaction>()
            .BuildServiceProvider();

    private static EventDispatcherService Dispatcher(IEventOutbox outbox, IServiceProvider services) =>
        new(outbox, services.GetRequiredService<IServiceScopeFactory>(), new EventRetryPolicy(2, TimeSpan.FromMilliseconds(5)),
            NullLogger<EventDispatcherService>.Instance);

    private static async Task Until(Func<bool> condition)
    {
        for (int i = 0; i < 200 && !condition(); i++) await Task.Delay(10);
        Assert.True(condition(), "delivery did not reach the expected state");
    }

    public sealed record ProbeHappened(Guid EventId, DateTimeOffset OccurredOn, string Content) : IDomainEvent;

    public sealed record NobodyReacts(Guid EventId, DateTimeOffset OccurredOn) : IDomainEvent;

    public sealed class Observed
    {
        public int Synchronous;
        public bool SynchronousInTransaction;
        public int Reactions;
        public bool ReactionInTransaction;
        public string? Payload;
    }

    /// <summary>The synchronous kind: a metric, inside the publishing transaction.</summary>
    public sealed class ProbeCounter(Observed observed, InMemoryTransactionBoundary boundary) : EventListener<ProbeHappened>
    {
        protected override Task OnAsync(ProbeHappened value, CancellationToken ct)
        {
            observed.Synchronous++;
            observed.SynchronousInTransaction = boundary.InTransaction;
            return Task.CompletedTask;
        }
    }

    /// <summary>The transactional kind: the reaction, after the commit.</summary>
    public sealed class ProbeReaction(Observed observed, InMemoryTransactionBoundary boundary) : TransactionalEventListener<ProbeHappened>
    {
        protected override Task OnAsync(ProbeHappened value, CancellationToken ct)
        {
            observed.ReactionInTransaction = boundary.InTransaction;
            observed.Payload = value.Content;
            Interlocked.Increment(ref observed.Reactions);
            return Task.CompletedTask;
        }
    }
}