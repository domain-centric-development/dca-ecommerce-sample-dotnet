using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using DcaShop.SharedKernel.Infrastructure.Transactions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>Storage survives dispatcher reconstruction within this process; it is not durable across process loss.</summary>
public sealed class InMemoryIntegrationEventOutbox : IIntegrationEventOutbox
{
    private readonly ConcurrentDictionary<Guid, IntegrationEventPublication> _publications = new();
    private readonly Channel<Guid> _due = Channel.CreateUnbounded<Guid>();
    private readonly TimeProvider _clock;
    public InMemoryIntegrationEventOutbox(TimeProvider clock) => _clock = clock;

    public IntegrationEventPublication Register(IIntegrationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var publication = new IntegrationEventPublication(@event.EventId, JsonSerializer.Serialize(@event, @event.GetType()),
            @event.GetType(), _clock.GetUtcNow(), PublicationStatus.Staged, 0, null, null);
        // Validate deserialization before registering an undeliverable snapshot.
        _ = publication.Event;
        return _publications.GetOrAdd(publication.Id, publication);
    }

    public void Commit(Guid id) => Update(id, p => p.Status == PublicationStatus.Staged ? p with { Status = PublicationStatus.Pending } : p);
    public void Release(Guid id) => _due.Writer.TryWrite(id);
    public void Requeue(Guid id) => Release(id);
    public void Discard(Guid id)
    {
        lock (InMemoryTransactionBoundary.CommitGate)
            if (_publications.TryGetValue(id, out var p) && (p.Status == PublicationStatus.Staged || p.Status == PublicationStatus.Pending)) _publications.TryRemove(id, out _);
    }

    public async IAsyncEnumerable<IntegrationEventPublication> ReadDueAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Every reconstructed dispatcher enumerates retained committed state; wakeups are dispensable.
        foreach (var publication in All().Where(p => p.Status == PublicationStatus.Pending)) Release(publication.Id);
        await foreach (var id in _due.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            IntegrationEventPublication? publication;
            lock (InMemoryTransactionBoundary.CommitGate) _publications.TryGetValue(id, out publication);
            if (publication?.Status != PublicationStatus.Pending) continue;
            var waiting = publication.Consumers.Values.Where(c => c.Status == PublicationStatus.Pending).ToArray();
            var next = waiting.Length == 0 ? _clock.GetUtcNow() : waiting.Min(c => c.NextAttemptOn ?? _clock.GetUtcNow());
            if (next > _clock.GetUtcNow()) { _ = WakeLater(id, next - _clock.GetUtcNow(), cancellationToken); continue; }
            yield return publication;
        }
    }

    private async Task WakeLater(Guid id, TimeSpan delay, CancellationToken token)
    {
        try { await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, _clock, token); Release(id); }
        catch (OperationCanceledException) { /* eligibility and due time remain in storage */ }
    }

    public IntegrationEventPublication InitializeConsumers(Guid id, IEnumerable<string> consumers) => Update(id, p =>
        p.ConsumersInitialized ? p : p with
        {
            ConsumersInitialized = true,
            Consumers = consumers.Distinct(StringComparer.Ordinal)
            .ToImmutableDictionary(c => c, c => new ConsumerDelivery(c, PublicationStatus.Pending, 0, null, null, null))
        });

    public void MarkConsumerCompleted(Guid id, string consumer) => Update(id, p => Progress(p with
    {
        Consumers = p.Consumers.SetItem(consumer, p.Consumers[consumer] with { Status = PublicationStatus.Completed, CompletedOn = _clock.GetUtcNow(), LastError = null, NextAttemptOn = null })
    }));

    public IntegrationEventPublication RecordConsumerFailure(Guid id, string consumer, string error, int maxAttempts, TimeSpan nextDelay) => Update(id, p =>
    {
        var state = p.Consumers[consumer]; var attempts = state.Attempts + 1;
        return Progress(p with
        {
            Attempts = p.Attempts + 1,
            LastError = error,
            Consumers = p.Consumers.SetItem(consumer,
            state with
            {
                Attempts = attempts,
                LastError = error,
                Status = attempts >= maxAttempts ? PublicationStatus.Failed : PublicationStatus.Pending,
                NextAttemptOn = attempts >= maxAttempts ? null : _clock.GetUtcNow() + nextDelay
            })
        });
    });

    public void MarkCompleted(Guid id) => Update(id, Progress);

    private IntegrationEventPublication Progress(IntegrationEventPublication p)
    {
        bool complete = p.ConsumersInitialized && p.Consumers.Values.All(c => c.Status == PublicationStatus.Completed);
        bool pending = !p.ConsumersInitialized || p.Consumers.Values.Any(c => c.Status == PublicationStatus.Pending);
        return p with
        {
            Status = complete ? PublicationStatus.Completed : pending ? PublicationStatus.Pending : PublicationStatus.Failed,
            CompletedOn = complete ? _clock.GetUtcNow() : null,
            LastError = complete ? null : p.LastError
        };
    }

    public void ReplayFailed(Guid id)
    {
        Update(id, p => !p.Consumers.Values.Any(c => c.Status == PublicationStatus.Failed) ? p : Progress(p with
        {
            Consumers = p.Consumers.ToImmutableDictionary(e => e.Key,
            e => e.Value.Status == PublicationStatus.Failed ? e.Value with { Status = PublicationStatus.Pending, Attempts = 0, NextAttemptOn = null } : e.Value)
        }));
        Release(id);
    }

    public IReadOnlyList<IntegrationEventPublication> All()
    {
        lock (InMemoryTransactionBoundary.CommitGate) return _publications.Values.OrderByDescending(p => p.RegisteredOn).ToList();
    }

    private IntegrationEventPublication Update(Guid id, Func<IntegrationEventPublication, IntegrationEventPublication> change)
    {
        lock (InMemoryTransactionBoundary.CommitGate)
        {
            if (!_publications.TryGetValue(id, out var current)) throw new KeyNotFoundException($"Unknown publication: {id}");
            return _publications[id] = change(current);
        }
    }
}
