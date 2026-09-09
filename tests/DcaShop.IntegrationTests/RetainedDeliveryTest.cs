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
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "specification/vectors/delivery.json")));
        foreach (var v in document.RootElement.EnumerateArray()) yield return new object[] { v.GetProperty("id").GetString()! };
    }
    [Theory, MemberData(nameof(SharedVectors))]
    public async Task SharedDeliveryVector(string id)
    {
        switch (id)
        {
            case "delivery.rollback.no-publication": await UncommittedAndRolledBackWorkCannotBeReplayed(true); break;
            case "delivery.startup.replays-committed-only": await UncommittedAndRolledBackWorkCannotBeReplayed(false); await ReconstructedDispatcherReplaysCommittedSnapshotWithOrWithoutWakeup(false); break;
            case "delivery.replay.snapshot": await ReconstructedDispatcherReplaysCommittedSnapshotWithOrWithoutWakeup(true); break;
            case "delivery.multi-consumer.partial-failure":
            case "delivery.retry.same-key":
            case "delivery.ack.provider-accepted-not-delivered":
            case "delivery.provider-idempotency.same-key": await AcceptanceBeforeAcknowledgementRetriesSameConsumerKeyAndSnapshot(true, 1); break;
            case "delivery.provider-no-idempotency.duplicate-possible": await AcceptanceBeforeAcknowledgementRetriesSameConsumerKeyAndSnapshot(false, 2); break;
            case "delivery.retry.exhausted-inspectable":
            case "delivery.manual-replay.failed-consumer-only": await ExhaustionIsInspectableAndManualReplayTouchesOnlyFailedConsumer(); break;
            default: Assert.Fail("Vector has no adapter: " + id); break;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReconstructedDispatcherReplaysCommittedSnapshotWithOrWithoutWakeup(bool wakeup)
    {
        var outbox = new InMemoryIntegrationEventOutbox(TimeProvider.System);
        var boundary = new InMemoryTransactionBoundary();
        var probe = new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "captured");
        var publisher = new OutboxIntegrationEventPublisher(outbox, new Hooks(boundary, wakeup));
        await boundary.InTransactionAsync(async ct => { await publisher.PublishAsync(probe, ct); return 0; });
        probe.Content = "changed later";
        var observed = new Observed();
        using var services = Services(observed);
        using var dispatcher = Dispatcher(outbox, services);
        await dispatcher.StartAsync(default);
        await Until(() => outbox.All().Single().Status == PublicationStatus.Completed);
        await dispatcher.StopAsync(default);
        Assert.Equal(1, observed.FirstCalls);
        Assert.All(observed.Payloads, value => Assert.Equal("captured", value));
        // Another dispatcher over the same retained store must not repeat acknowledged work.
        using var reconstructed = Dispatcher(outbox, services);
        await reconstructed.StartAsync(default);
        await Task.Delay(30);
        await reconstructed.StopAsync(default);
        Assert.Equal(1, observed.FirstCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UncommittedAndRolledBackWorkCannotBeReplayed(bool rollback)
    {
        var outbox = new InMemoryIntegrationEventOutbox(TimeProvider.System);
        var boundary = new InMemoryTransactionBoundary();
        var publisher = new OutboxIntegrationEventPublisher(outbox, boundary);
        var observed = new Observed();
        using var services = Services(observed);
        using var dispatcher = Dispatcher(outbox, services);
        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.InTransactionAsync<int>(async ct =>
        {
            await publisher.PublishAsync(new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "staged"), ct);
            if (!rollback)
            {
                await dispatcher.StartAsync(default);
                await Task.Delay(30);
                Assert.Equal(0, observed.FirstCalls);
                Assert.Equal(PublicationStatus.Staged, outbox.All().Single().Status);
            }
            throw new InvalidOperationException("rollback");
        }));
        if (rollback) { await dispatcher.StartAsync(default); await Task.Delay(30); }
        await dispatcher.StopAsync(default);
        Assert.Empty(outbox.All()); Assert.Equal(0, observed.FirstCalls);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    public async Task AcceptanceBeforeAcknowledgementRetriesSameConsumerKeyAndSnapshot(bool providerIdempotency, int externalEffects)
    {
        var outbox = new InMemoryIntegrationEventOutbox(TimeProvider.System);
        var observed = new Observed { CrashAfterAcceptance = true, ProviderIdempotency = providerIdempotency };
        using var services = Services(observed);
        var probe = new ProbeEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "original");
        await new OutboxIntegrationEventPublisher(outbox, new InMemoryTransactionBoundary()).PublishAsync(probe);
        probe.Content = "current business state";
        using var dispatcher = Dispatcher(outbox, services);
        await dispatcher.StartAsync(default);
        await Until(() => outbox.All().Single().Status == PublicationStatus.Completed);
        await dispatcher.StopAsync(default);
        Assert.Equal(1, observed.FirstCalls); Assert.Equal(2, observed.SecondCalls);
        Assert.Equal(externalEffects, observed.ExternalEffects);
        Assert.Single(observed.Keys.Distinct());
        Assert.All(observed.Payloads, payload => Assert.Equal("original", payload));
        Assert.Equal(0, observed.InboxDeliveries); // provider acceptance, not inbox delivery, acknowledges work
        Assert.All(outbox.All().Single().Consumers.Values, c => Assert.Equal(PublicationStatus.Completed, c.Status));
    }

    [Fact]
    public async Task ExhaustionIsInspectableAndManualReplayTouchesOnlyFailedConsumer()
    {
        var outbox = new InMemoryIntegrationEventOutbox(TimeProvider.System);
        var observed = new Observed { AlwaysFail = true };
        using var services = Services(observed);
        var id = Guid.NewGuid();
        await new OutboxIntegrationEventPublisher(outbox, new InMemoryTransactionBoundary()).PublishAsync(new ProbeEvent(id, DateTimeOffset.UtcNow, "original"));
        using var dispatcher = Dispatcher(outbox, services);
        await dispatcher.StartAsync(default);
        await Until(() => outbox.All().Single().Status == PublicationStatus.Failed);
        var failed = outbox.All().Single();
        Assert.Equal(2, failed.Consumers.Values.Single(c => c.Status == PublicationStatus.Failed).Attempts);
        Assert.NotNull(failed.LastError); Assert.Equal(1, observed.FirstCalls);
        observed.AlwaysFail = false;
        outbox.ReplayFailed(id);
        await Until(() => outbox.All().Single().Status == PublicationStatus.Completed);
        await dispatcher.StopAsync(default);
        Assert.Equal(1, observed.FirstCalls); Assert.Equal(3, observed.SecondCalls);
        Assert.Single(observed.Keys.Distinct());
        Assert.Equal(failed.Payload, outbox.All().Single().Payload);
    }

    private static ServiceProvider Services(Observed observed) => new ServiceCollection().AddSingleton(observed)
        .AddScoped<IEventListener, FirstListener>().AddScoped<IEventListener, SecondListener>().BuildServiceProvider();
    private static IntegrationEventDispatcherService Dispatcher(IIntegrationEventOutbox outbox, IServiceProvider services) =>
        new(outbox, services.GetRequiredService<IServiceScopeFactory>(), new IntegrationEventRetryPolicy(2, TimeSpan.FromMilliseconds(5)), NullLogger<IntegrationEventDispatcherService>.Instance);
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
    public sealed class FirstListener(Observed observed) : EventListener<ProbeEvent>
    {
        protected override Task OnAsync(ProbeEvent value, CancellationToken ct) { observed.FirstCalls++; observed.Payloads.Add(value.Content); return Task.CompletedTask; }
    }
    public sealed class SecondListener(Observed observed) : EventListener<ProbeEvent>
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
    private sealed class Hooks(InMemoryTransactionBoundary boundary, bool wakeup) : ITransactionHooks
    {
        public bool InTransaction => boundary.InTransaction;
        public void EnlistCommit(Action action) => boundary.EnlistCommit(action);
        public void AfterCommit(Action action) { if (wakeup) boundary.AfterCommit(action); }
        public void AfterRollback(Action action) => boundary.AfterRollback(action);
    }
}
