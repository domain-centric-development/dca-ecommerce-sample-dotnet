using DomainCentric.BuildingBlocks.Ddd.Tactical;
namespace DcaShop.SharedKernel.Infrastructure.Events;

/// <summary>
/// Retained publication snapshots with per-consumer delivery state. A publication is recorded in the transaction of
/// the work that caused it, so it exists exactly when that work committed; <see cref="Release"/> is only a wakeup.
/// </summary>
public interface IEventOutbox
{
    Task<EventPublication> RegisterAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
    /// <summary>Captures a domain event for the transactional listeners of the publishing context.</summary>
    Task<EventPublication> RegisterAsync(IDomainEvent @event, CancellationToken cancellationToken = default);
    /// <summary>Wakes the dispatcher for a committed publication.</summary>
    void Release(Guid publicationId);
    IAsyncEnumerable<EventPublication> ReadDueAsync(CancellationToken cancellationToken = default);
    Task<EventPublication> InitializeConsumersAsync(Guid publicationId, IEnumerable<string> consumers, CancellationToken cancellationToken = default);
    Task MarkConsumerCompletedAsync(Guid publicationId, string consumer, CancellationToken cancellationToken = default);
    Task<EventPublication> RecordConsumerFailureAsync(Guid publicationId, string consumer, string error, int maxAttempts, TimeSpan nextDelay, CancellationToken cancellationToken = default);
    Task<EventPublication> MarkCompletedAsync(Guid publicationId, CancellationToken cancellationToken = default);
    /// <summary>Retries failed consumers only, retaining their keys and the original event payload.</summary>
    Task ReplayFailedAsync(Guid publicationId, CancellationToken cancellationToken = default);
    /// <summary>Retries one failed consumer only; the others keep their state.</summary>
    Task ReplayFailedAsync(Guid publicationId, string consumer, CancellationToken cancellationToken = default);
    Task<EventPublication?> FindAsync(Guid publicationId, CancellationToken cancellationToken = default);
    /// <summary>Every publication, newest first.</summary>
    Task<IReadOnlyList<EventPublication>> AllAsync(CancellationToken cancellationToken = default);
}