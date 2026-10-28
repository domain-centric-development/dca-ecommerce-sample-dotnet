using DomainCentric.BuildingBlocks.Ddd.Tactical;
namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>Retained in-process publication snapshots: commit establishes eligibility; Release is only a wakeup.</summary>
public interface IEventOutbox
{
    EventPublication Register(IIntegrationEvent @event);
    /// <summary>Captures a domain event for the transactional listeners of the publishing context.</summary>
    EventPublication Register(IDomainEvent @event);
    void Commit(Guid publicationId);
    void Release(Guid publicationId);
    void Discard(Guid publicationId);
    IAsyncEnumerable<EventPublication> ReadDueAsync(CancellationToken cancellationToken = default);
    EventPublication InitializeConsumers(Guid publicationId, IEnumerable<string> consumers);
    void MarkConsumerCompleted(Guid publicationId, string consumer);
    EventPublication RecordConsumerFailure(Guid publicationId, string consumer, string error, int maxAttempts, TimeSpan nextDelay);
    void MarkCompleted(Guid publicationId);
    void Requeue(Guid publicationId);
    /// <summary>Retries failed consumers only, retaining their keys and the original event payload.</summary>
    void ReplayFailed(Guid publicationId);
    /// <summary>Retries one failed consumer only; the others keep their state.</summary>
    void ReplayFailed(Guid publicationId, string consumer);
    IReadOnlyList<EventPublication> All();
}