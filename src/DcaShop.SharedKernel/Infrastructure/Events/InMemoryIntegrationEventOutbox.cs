using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// In-memory outbox: publications live in a dictionary, a channel of ids wakes the dispatcher. Durable within
/// the process only — a database-backed implementation persists the publication in the aggregate's transaction
/// and keeps the same interface (<c>Discard</c> becomes a no-op there: the database rolls the row back).
/// </summary>
public sealed class InMemoryIntegrationEventOutbox : IIntegrationEventOutbox
{
    private readonly ConcurrentDictionary<Guid, IntegrationEventPublication> _publications = new();
    private readonly Channel<Guid> _due = Channel.CreateUnbounded<Guid>();
    private readonly TimeProvider _clock;
    private int _replayed;

    public InMemoryIntegrationEventOutbox(TimeProvider clock)
    {
        _clock = clock;
    }

    public IntegrationEventPublication Register(IIntegrationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var publication = new IntegrationEventPublication(@event.EventId, @event, _clock.GetUtcNow(), PublicationStatus.Pending, 0, null, null);
        _publications[publication.Id] = publication;
        return publication;
    }

    public void Release(Guid publicationId) => _due.Writer.TryWrite(publicationId);

    public void Discard(Guid publicationId) => _publications.TryRemove(publicationId, out _);

    public async IAsyncEnumerable<IntegrationEventPublication> ReadDueAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _replayed, 1) == 0)
        {
            // Replay whatever was registered before the dispatcher started (Spring Modulith's
            // republish-outstanding-events-on-restart, scaled down to one process).
            foreach (var outstanding in _publications.Values.Where(p => p.Status == PublicationStatus.Pending))
            {
                _due.Writer.TryWrite(outstanding.Id);
            }
        }

        await foreach (var id in _due.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_publications.TryGetValue(id, out var publication) && publication.Status == PublicationStatus.Pending)
            {
                yield return publication;
            }
        }
    }

    public void MarkCompleted(Guid publicationId) =>
        Update(publicationId, p => p with { Status = PublicationStatus.Completed, LastError = null, CompletedOn = _clock.GetUtcNow() });

    public IntegrationEventPublication RecordFailure(Guid publicationId, string error) =>
        Update(publicationId, p => p with { Attempts = p.Attempts + 1, LastError = error });

    public void Requeue(Guid publicationId) => _due.Writer.TryWrite(publicationId);

    public void MarkFailed(Guid publicationId) =>
        Update(publicationId, p => p with { Status = PublicationStatus.Failed });

    public IReadOnlyList<IntegrationEventPublication> All() =>
        _publications.Values.OrderByDescending(p => p.RegisteredOn).ToList();

    private IntegrationEventPublication Update(Guid id, Func<IntegrationEventPublication, IntegrationEventPublication> change)
    {
        while (true)
        {
            if (!_publications.TryGetValue(id, out var current))
            {
                throw new KeyNotFoundException($"Unknown publication: {id}");
            }

            var updated = change(current);
            if (_publications.TryUpdate(id, updated, current))
            {
                return updated;
            }
        }
    }
}
