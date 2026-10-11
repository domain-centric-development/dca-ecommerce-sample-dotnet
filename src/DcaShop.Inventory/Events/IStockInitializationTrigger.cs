using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Inventory.Events;

/// <summary>
/// Consumer-defined contract (interface inversion): Inventory creates a stock record when an integration event
/// carrying this shape arrives. The catalog's <c>ProductCreatedEvent</c> implements it; Inventory never depends
/// on the Product context.
/// It extends <see cref="IIntegrationEvent"/>: delivery is at least once, so the consumer deduplicates on the
/// <see cref="IIntegrationEvent.EventId"/> the contract hands it.
/// </summary>
public interface IStockInitializationTrigger : IIntegrationEvent
{
    Guid ProductId { get; }

    int InitialStock { get; }
}