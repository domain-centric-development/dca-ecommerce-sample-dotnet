using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Event;

public sealed record ProductCategoryChanged(Guid EventId, DateTimeOffset OccurredOn, ProductId ProductId, Category OldCategory, Category NewCategory) : IDomainEvent
{
    public static ProductCategoryChanged Now(ProductId productId, Category oldCategory, Category newCategory) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, productId, oldCategory, newCategory);
}
