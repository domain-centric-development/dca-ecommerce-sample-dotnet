using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// Outbox for integration events: publishing registers a publication, the dispatcher delivers it and marks it
/// completed — or records the failure and requeues it. Nothing is dropped silently; every publication is
/// inspectable until completed, and outstanding ones are replayed when the dispatcher starts.
/// </summary>
public interface IIntegrationEventOutbox
{
    /// <summary>Records a new publication and queues it for delivery.</summary>
    IntegrationEventPublication Register(IIntegrationEvent @event);

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
