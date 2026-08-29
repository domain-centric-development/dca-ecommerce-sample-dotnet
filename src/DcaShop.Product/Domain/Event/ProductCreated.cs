using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Event;

/// <summary>A new product was created; carries initial price and stock for the Pricing and Inventory contexts.</summary>
public sealed record ProductCreated(
    Guid EventId,
    DateTimeOffset OccurredOn,
    ProductId ProductId,
    Sku Sku,
    ProductName Name,
    Category Category,
    Price InitialPrice,
    int InitialStock) : IDomainEvent
{
    public static ProductCreated Now(ProductId productId, Sku sku, ProductName name, Category category, Price initialPrice, int initialStock) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, productId, sku, name, category, initialPrice, initialStock);
}
