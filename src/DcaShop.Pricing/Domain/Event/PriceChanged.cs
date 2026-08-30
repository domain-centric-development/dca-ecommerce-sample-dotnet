using DcaShop.Pricing.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Pricing.Domain.Event;

/// <summary>The price of a product changed; both the old and the new price are part of the record.</summary>
public sealed record PriceChanged(
    Guid EventId,
    DateTimeOffset OccurredOn,
    PriceId PriceId,
    ProductId ProductId,
    Money OldPrice,
    Money NewPrice,
    DateTimeOffset EffectiveFrom) : IDomainEvent
{
    public static PriceChanged Now(PriceId priceId, ProductId productId, Money oldPrice, Money newPrice, DateTimeOffset effectiveFrom) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, priceId, productId, oldPrice, newPrice, effectiveFrom);
}
