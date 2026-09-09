using DomainCentric.BuildingBlocks.Ddd.Tactical;
namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>Retained in-process publication snapshots: commit establishes eligibility; Release is only a wakeup.</summary>
public interface IIntegrationEventOutbox
{
    IntegrationEventPublication Register(IIntegrationEvent @event);
    void Commit(Guid publicationId);
    void Release(Guid publicationId);
    void Discard(Guid publicationId);
    IAsyncEnumerable<IntegrationEventPublication> ReadDueAsync(CancellationToken cancellationToken = default);
    IntegrationEventPublication InitializeConsumers(Guid publicationId, IEnumerable<string> consumers);
    void MarkConsumerCompleted(Guid publicationId, string consumer);
    IntegrationEventPublication RecordConsumerFailure(Guid publicationId, string consumer, string error, int maxAttempts, TimeSpan nextDelay);
    void MarkCompleted(Guid publicationId);
    void Requeue(Guid publicationId);
    /// <summary>Retries failed consumers only, retaining their keys and the original event payload.</summary>
    void ReplayFailed(Guid publicationId);
    IReadOnlyList<IntegrationEventPublication> All();
}
