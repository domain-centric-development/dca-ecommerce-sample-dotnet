using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.Shared;

/// <summary>
/// The integration events this context has already turned into an effect, keyed by the effect and the event id:
/// two effects may react to the same event, and each happens exactly once. Delivery is at least once, so the use
/// case that produces the effect asks before it acts and records the pair after the effect, in the same transaction.
/// A <see cref="IStore"/>, not a repository: an entry is a record of handling, not an aggregate.
/// </summary>
public interface IProcessedEvents : IStore
{
    /// <summary>Whether <paramref name="effect"/> has already been produced for the event with this id.</summary>
    Task<bool> ContainsAsync(string effect, Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Records <paramref name="effect"/> as produced for the event with this id.</summary>
    Task MarkProcessedAsync(string effect, Guid eventId, CancellationToken cancellationToken = default);
}