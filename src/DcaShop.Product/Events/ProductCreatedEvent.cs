using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Events;

/// <summary>Published language of the catalog: a product exists now. Pricing and Inventory initialise their records from it.</summary>
[IntegrationEventType("product-created", Version = 1)]
public sealed record ProductCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    ProductId ProductId,
    string Sku,
    string Name,
    string Category,
    Money InitialPrice,
    int InitialStock) : IIntegrationEvent;
