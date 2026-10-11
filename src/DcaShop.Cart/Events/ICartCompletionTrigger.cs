using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Events;

/// <summary>
/// Consumer-defined contract (interface inversion): the cart completes when an event carrying this shape arrives.
/// Checkout's <c>CheckoutConfirmedEvent</c> implements it; the cart never depends on Checkout.
/// It extends <see cref="IIntegrationEvent"/>: delivery is at least once, so the consumer deduplicates on the
/// <see cref="IIntegrationEvent.EventId"/> the contract hands it.
/// </summary>
public interface ICartCompletionTrigger : IIntegrationEvent
{
    string CartId { get; }
    string SessionId { get; }
    IReadOnlyList<string> PurchasedPositions { get; }
}