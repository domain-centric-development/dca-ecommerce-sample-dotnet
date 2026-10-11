using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.Shared;

/// <summary>
/// The integration events this context has already handled, by event id. Delivery is at least once, so a consumer
/// asks before it acts and records the id after its use case succeeded; a redelivered event is then skipped.
/// A <see cref="IStore"/>, not a repository: an entry is a record of handling, not an aggregate.
/// </summary>
public interface IProcessedEvents : IStore
{
    /// <summary>Whether the event with this id has already been handled.</summary>
    Task<bool> ContainsAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Records the event with this id as handled.</summary>
    Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
}