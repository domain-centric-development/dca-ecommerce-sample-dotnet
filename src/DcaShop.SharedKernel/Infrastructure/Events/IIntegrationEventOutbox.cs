using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// Outbox for integration events. Publishing registers a publication <em>inside</em> the aggregate's transaction
/// (a database implementation writes the outbox row in that same transaction); <see cref="Release"/> after commit
/// hands it to the dispatcher, <see cref="Discard"/> after rollback removes it again where the store cannot roll
/// back by itself. The dispatcher delivers and marks completed — or records the failure and requeues. Nothing is
/// dropped silently; every publication is inspectable until completed, and outstanding ones are replayed when the
/// dispatcher starts.
/// </summary>
public interface IIntegrationEventOutbox
{
    /// <summary>Records a new publication (pending, not yet due). Call inside the aggregate's transaction.</summary>
    IntegrationEventPublication Register(IIntegrationEvent @event);

    /// <summary>Queues a registered publication for delivery. Call after the transaction committed.</summary>
    void Release(Guid publicationId);

    /// <summary>Removes a publication whose transaction rolled back.</summary>
    void Discard(Guid publicationId);

    /// <summary>Streams publications that are due for delivery, starting with everything still outstanding.</summary>
    IAsyncEnumerable<IntegrationEventPublication> ReadDueAsync(CancellationToken cancellationToken = default);

    /// <summary>Marks a publication as delivered to every listener.</summary>
    void MarkCompleted(Guid publicationId);

    /// <summary>Records a failed delivery attempt; returns the updated publication (still pending).</summary>
    IntegrationEventPublication RecordFailure(Guid publicationId, string error);

    /// <summary>Queues a pending publication for another delivery attempt.</summary>
    void Requeue(Guid publicationId);

    /// <summary>Gives up on a publication: it stays visible as <see cref="PublicationStatus.Failed"/>.</summary>
    void MarkFailed(Guid publicationId);

    /// <summary>All publications the outbox knows about, newest first.</summary>
    IReadOnlyList<IntegrationEventPublication> All();
}
