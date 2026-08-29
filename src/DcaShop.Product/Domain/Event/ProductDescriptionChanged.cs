using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Event;

public sealed record ProductDescriptionChanged(Guid EventId, DateTimeOffset OccurredOn, ProductId ProductId, ProductDescription OldDescription, ProductDescription NewDescription) : IDomainEvent
{
    public static ProductDescriptionChanged Now(ProductId productId, ProductDescription oldDescription, ProductDescription newDescription) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, productId, oldDescription, newDescription);
}
