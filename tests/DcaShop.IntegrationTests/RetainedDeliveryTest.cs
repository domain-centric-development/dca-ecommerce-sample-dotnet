using DcaShop.Infrastructure.Events;
using DcaShop.SharedKernel.Adapter.Outgoing.Event;
using DcaShop.SharedKernel.Infrastructure.Events;
using DcaShop.SharedKernel.Infrastructure.Transactions;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DcaShop.IntegrationTests;

public sealed class RetainedDeliveryTest
{
    public static IEnumerable<object[]> SharedVectors()
    {
        if (!SpecificationVectors.Present) yield break;
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "specification/vectors/delivery.json")));
        foreach (var v in document.RootElement.EnumerateArray()) yield return new object[] { v.GetProperty("id").GetString()! };
    }
    /// <summary>Each vector names one claim, and one method asserts it, so a vector fails on its own.</summary>
    [SpecificationTheory, MemberData(nameof(SharedVectors))]
    public async Task SharedDeliveryVector(string id)
    {
        switch (id)
        {
            case "delivery.rollback.no-publication": await RollbackLeavesNoPublication(); break;
            case "delivery.startup.replays-committed-only": await StartupReplaysCommittedOnly(); break;
            case "delivery.multi-consumer.partial-failure": await PartialFailureRetriesOnlyTheFailedConsumer(); break;
            case "delivery.retry.same-key": await RetryKeepsTheSameKey(); break;
            case "delivery.replay.snapshot": await ReplayDeliversTheSnapshot(); break;
            case "delivery.ack.provider-accepted-not-delivered": await AcceptanceWithoutAcknowledgementIsRetried(); break;
            case "delivery.retry.exhausted-inspectable": await ExhaustedDeliveryIsInspectable(); break;
            case "delivery.manual-replay.failed-consumer-only": await ManualReplayTouchesOnlyTheFailedConsumer(); break;
            case "delivery.provider-idempotency.same-key": await IdempotentProviderMakesOneEffect(); break;
            case "delivery.provider-no-idempotency.duplicate-possible": await ProviderWithoutIdempotencyMayMakeTwoEffects(); break;
            default: Assert.Fail("Vector has no adapter: " + id); break;
        }
    }

    [Fact]
    public async Task RollbackLeavesNoPublication()
    {
        using var store = new IsolatedDatabase();
        var outbox = store.Outbox;
        var boundary = store.Boundary;
        var publisher = new OutboxIntegrationEventPublisher(outbox, boundary);
        var survivor = new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "committed outside a transaction");
        await publisher.PublishAsync(survivor);
        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.InTransactionAsync<int>(async ct =>
        {
            await publisher.PublishAsync(new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "rolled back"), ct);
            throw new InvalidOperationException("rollback");
        }));
        Assert.Equal(1, store.Committed());
        Assert.Equal(survivor.EventId, outbox.All().Single().Id);
        var observed = new Observed();
        using var services = Services(observed);
        using var dispatcher = Dispatcher(outbox, services);
        await dispatcher.StartAsync(default);
        await Until(() => outbox.All().Single().Status == PublicationStatus.Completed);
        await Task.Delay(30);
        await dispatcher.StopAsync(default);
        Assert.Equal(1, observed.FirstCalls);
        Assert.All(observed.Payloads, payload => Assert.Equal("committed outside a transaction", payload));
    }

    [Fact]
    public async Task StartupReplaysCommittedOnly()
    {
        using var store = new IsolatedDatabase();
        var outbox = store.Outbox;
        var boundary = store.Boundary;
        // no wakeup: only a dispatcher reading the store when it starts can deliver
        var publisher = new OutboxIntegrationEventPublisher(outbox, new Hooks(boundary, wakeup: false));
        await boundary.InTransactionAsync(async ct => { await publisher.PublishAsync(new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "committed"), ct); return 0; });
        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.InTransactionAsync<int>(async ct =>
        {
            await publisher.PublishAsync(new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "rolled back"), ct);
            throw new InvalidOperationException("rollback");
        }));
        var observed = new Observed();
        using var services = Services(observed);
        using var dispatcher = Dispatcher(outbox, services);
        // the dispatcher starts before the next transaction, so it never runs inside it
        await dispatcher.StartAsync(default);
        await Until(() => outbox.All().Single().Status == PublicationStatus.Completed);
        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.InTransactionAsync<int>(async ct =>
        {
            await publisher.PublishAsync(new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "uncommitted"), ct);
            await Task.Delay(30, ct);
            Assert.Equal(1, observed.FirstCalls);
            Assert.Equal(1, store.Committed());
            throw new InvalidOperationException("rollback");
        }));
        await Task.Delay(30);
        await dispatcher.StopAsync(default);
        Assert.Equal(1, observed.FirstCalls);
        Assert.All(observed.Payloads, payload => Assert.Equal("committed", payload));
        Assert.Single(outbox.All());
        // Another dispatcher over the same retained store must not repeat acknowledged work.
        using var reconstructed = Dispatcher(outbox, services);
        await reconstructed.StartAsync(default);
        await Task.Delay(30);
        await reconstructed.StopAsync(default);
        Assert.Equal(1, observed.FirstCalls);
    }

    [Fact]
    public async Task PartialFailureRetriesOnlyTheFailedConsumer()
    {
        var (observed, publication) = await DeliverWithCrashAfterAcceptance(providerIdempotency: true);
        Assert.Equal(1, observed.FirstCalls);
        Assert.Equal(2, observed.SecondCalls);
        Assert.Equal(0, publication.Consumers[typeof(FirstListener).FullName!].Attempts);
        Assert.Equal(1, publication.Consumers[typeof(SecondListener).FullName!].Attempts);
        Assert.All(publication.Consumers.Values, c => Assert.Equal(PublicationStatus.Completed, c.Status));
    }

    [Fact]
    public async Task RetryKeepsTheSameKey()
    {
        var (observed, publication) = await DeliverWithCrashAfterAcceptance(providerIdempotency: true);
        Assert.Equal(2, observed.Keys.Count);
        Assert.All(observed.Keys, key => Assert.Equal(DeliveryIdentity.For(publication.Id, typeof(SecondListener).FullName!, "provider-effect"), key));
    }

    [Fact]
    public async Task ReplayDeliversTheSnapshot()
    {
        using var store = new IsolatedDatabase();
        var outbox = store.Outbox;
        var boundary = store.Boundary;
        var observed = new Observed { CrashAfterAcceptance = true, ProviderIdempotency = true };
        using var services = Services(observed);
        using var dispatcher = Dispatcher(outbox, services);
        await dispatcher.StartAsync(default);
        var probe = new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "captured");
        var publisher = new OutboxIntegrationEventPublisher(outbox, new Hooks(boundary, wakeup: true));
        await boundary.InTransactionAsync(async ct => { await publisher.PublishAsync(probe, ct); probe.Content = "changed in the same transaction"; return 0; });
        probe.Content = "changed later";
        await Until(() => outbox.All().Single().Status == PublicationStatus.Completed);
        await dispatcher.StopAsync(default);
        Assert.Equal(2, observed.SecondCalls);
        Assert.Equal(3, observed.Payloads.Count);
        Assert.All(observed.Payloads, payload => Assert.Equal("captured", payload));
    }

    [Fact]
    public async Task AcceptanceWithoutAcknowledgementIsRetried()
    {
        var (observed, publication) = await DeliverWithCrashAfterAcceptance(providerIdempotency: true);
        Assert.Equal(2, observed.SecondCalls);
        Assert.Equal(PublicationStatus.Completed, publication.Status);
        Assert.Equal(PublicationStatus.Completed, publication.Consumers[typeof(SecondListener).FullName!].Status);
        Assert.Equal(0, observed.InboxDeliveries); // provider acceptance, not inbox delivery, acknowledges work
    }

    [Fact]
    public async Task ExhaustedDeliveryIsInspectable()
    {
        using var store = new IsolatedDatabase();
        var outbox = store.Outbox;
        var observed = new Observed { AlwaysFail = true };
        using var services = Services(observed);
        await new OutboxIntegrationEventPublisher(outbox, store.Boundary).PublishAsync(new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "original"));
        using var dispatcher = Dispatcher(outbox, services);
        await dispatcher.StartAsync(default);
        await Until(() => outbox.All().Single().Status == PublicationStatus.Failed);
        await Task.Delay(50);
        await dispatcher.StopAsync(default);
        var failed = outbox.All().Single();
        Assert.Equal(PublicationStatus.Failed, failed.Status);
        var consumer = failed.Consumers[typeof(SecondListener).FullName!];
        Assert.Equal(PublicationStatus.Failed, consumer.Status);
        Assert.Equal(2, consumer.Attempts);
        Assert.NotNull(failed.LastError);
        Assert.Contains("original", failed.Payload);
        Assert.Equal(2, observed.SecondCalls); // not retried automatically after the bounded attempts
        Assert.Equal(1, observed.FirstCalls);
    }

    [Fact]
    public async Task ManualReplayTouchesOnlyTheFailedConsumer()
    {
        using var store = new IsolatedDatabase();
        var outbox = store.Outbox;
        var observed = new Observed { AlwaysFail = true };
        using var services = Services(observed);
        var id = Guid.NewGuid();
        await new OutboxIntegrationEventPublisher(outbox, store.Boundary).PublishAsync(new ProbeEvent(id, DateTimeOffset.UtcNow, "original"));
        using var dispatcher = Dispatcher(outbox, services);
        await dispatcher.StartAsync(default);
        await Until(() => outbox.All().Single().Status == PublicationStatus.Failed);
        var failed = outbox.All().Single();
        observed.AlwaysFail = false;
        await outbox.ReplayFailedAsync(id);
        await Until(() => outbox.All().Single().Status == PublicationStatus.Completed);
        await dispatcher.StopAsync(default);
        Assert.Equal(1, observed.FirstCalls);
        Assert.Equal(3, observed.SecondCalls);
        Assert.Single(observed.Keys.Distinct());
        Assert.Equal(failed.Payload, outbox.All().Single().Payload);
    }

    [Fact]
    public async Task IdempotentProviderMakesOneEffect()
    {
        var (observed, _) = await DeliverWithCrashAfterAcceptance(providerIdempotency: true);
        Assert.Equal(1, observed.ExternalEffects);
    }

    [Fact]
    public async Task ProviderWithoutIdempotencyMayMakeTwoEffects()
    {
        var (observed, _) = await DeliverWithCrashAfterAcceptance(providerIdempotency: false);
        Assert.Equal(2, observed.ExternalEffects);
    }

    /// <summary>The second listener reaches the provider and crashes before its local acknowledgement once; the retry
    /// completes it. The event is changed after publication, so a retry that re-read it would show.</summary>
    private static async Task<(Observed Observed, EventPublication Publication)> DeliverWithCrashAfterAcceptance(bool providerIdempotency)
    {
        using var store = new IsolatedDatabase();
        var outbox = store.Outbox;
        var observed = new Observed { CrashAfterAcceptance = true, ProviderIdempotency = providerIdempotency };
        using var services = Services(observed);
        var probe = new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "original");
        await new OutboxIntegrationEventPublisher(outbox, store.Boundary).PublishAsync(probe);
        probe.Content = "current business state";
        using var dispatcher = Dispatcher(outbox, services);
        await dispatcher.StartAsync(default);
        await Until(() => outbox.All().Single().Status == PublicationStatus.Completed);
        await dispatcher.StopAsync(default);
        return (observed, outbox.All().Single());
    }

    private static ServiceProvider Services(Observed observed) => new ServiceCollection().AddSingleton(observed)
        .AddScoped<IEventListener, FirstListener>().AddScoped<IEventListener, SecondListener>().BuildServiceProvider();
    private static EventDispatcherService Dispatcher(IEventOutbox outbox, IServiceProvider services) =>
        new(outbox, services.GetRequiredService<IServiceScopeFactory>(), new EventRetryPolicy(2, TimeSpan.FromMilliseconds(5)), NullLogger<EventDispatcherService>.Instance);
    private static async Task Until(Func<bool> condition)
    {
        for (int i = 0; i < 200 && !condition(); i++) await Task.Delay(10);
        Assert.True(condition(), "delivery did not reach the expected state");
    }
    public sealed class ProbeEvent(Guid eventId, DateTimeOffset occurredOn, string content) : IIntegrationEvent
    {
        public Guid EventId { get; } = eventId;
        public DateTimeOffset OccurredOn { get; } = occurredOn;
        public string Content { get; set; } = content;
    }
    public sealed class Observed
    {
        public int FirstCalls; public int SecondCalls; public int ExternalEffects; public int InboxDeliveries;
        public bool AlwaysFail; public bool CrashAfterAcceptance; public bool ProviderIdempotency;
        public List<string> Keys { get; } = new(); public List<string> Payloads { get; } = new();
        public HashSet<string> Accepted { get; } = new();
    }
    public sealed class FirstListener(Observed observed) : TransactionalEventListener<ProbeEvent>
    {
        protected override Task OnAsync(ProbeEvent value, CancellationToken ct) { observed.FirstCalls++; observed.Payloads.Add(value.Content); return Task.CompletedTask; }
    }
    public sealed class SecondListener(Observed observed) : TransactionalEventListener<ProbeEvent>
    {
        protected override Task OnAsync(ProbeEvent value, CancellationToken ct)
        {
            observed.SecondCalls++; observed.Payloads.Add(value.Content);
            var key = DeliveryIdentity.For(value.EventId, GetType().FullName!, "provider-effect"); observed.Keys.Add(key);
            if (observed.AlwaysFail) throw new InvalidOperationException("provider unavailable");
            if (!observed.ProviderIdempotency || observed.Accepted.Add(key)) observed.ExternalEffects++;
            if (observed.CrashAfterAcceptance && observed.SecondCalls == 1) throw new InvalidOperationException("crash before local acknowledgement");
            return Task.CompletedTask;
        }
    }
    private sealed class Hooks(PostgresTransactionBoundary boundary, bool wakeup) : ITransactionHooks
    {
        public bool InTransaction => boundary.InTransaction;
        public void AfterCommit(Action action) { if (wakeup) boundary.AfterCommit(action); }
        public void AfterRollback(Action action) => boundary.AfterRollback(action);
    }
}