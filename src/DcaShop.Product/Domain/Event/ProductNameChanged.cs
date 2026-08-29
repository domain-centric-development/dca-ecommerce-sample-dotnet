using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Event;

public sealed record ProductNameChanged(Guid EventId, DateTimeOffset OccurredOn, ProductId ProductId, ProductName OldName, ProductName NewName) : IDomainEvent
{
    public static ProductNameChanged Now(ProductId productId, ProductName oldName, ProductName newName) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, productId, oldName, newName);
}
