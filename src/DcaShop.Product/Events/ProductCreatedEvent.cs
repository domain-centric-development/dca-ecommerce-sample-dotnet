using DcaShop.Inventory.Events;
using DcaShop.Pricing.Events;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Events;

/// <summary>
/// Published language of the catalog: a product exists now. It implements the consumer-defined contracts of
/// Pricing and Inventory, so both initialise their records from this event without either context depending on
/// the catalog.
/// </summary>
[IntegrationEventType("product-created", Version = 1)]
public sealed record ProductCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    ProductId ProductId,
    string Sku,
    string Name,
    string Category,
    Money InitialPrice,
    int InitialStock) : IIntegrationEvent, IPriceInitializationTrigger, IStockInitializationTrigger;
