using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Pricing.Events;

/// <summary>
/// Consumer-defined contract (interface inversion): Pricing creates a price record when an integration event
/// carrying this shape arrives. The catalog's <c>ProductCreatedEvent</c> implements it; Pricing never depends
/// on the Product context.
/// It extends <see cref="IIntegrationEvent"/>: delivery is at least once, so the consumer deduplicates on the
/// <see cref="IIntegrationEvent.EventId"/> the contract hands it.
/// </summary>
public interface IPriceInitializationTrigger : IIntegrationEvent
{
    Guid ProductId { get; }

    string Amount { get; }

    string Currency { get; }
}