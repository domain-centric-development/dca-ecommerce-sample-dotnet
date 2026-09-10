using DcaShop.Pricing.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Pricing.Domain.Event;

/// <summary>A product got its first price.</summary>
public sealed record PriceCreated(
    Guid EventId,
    DateTimeOffset OccurredOn,
    PriceId PriceId,
    ProductId ProductId,
    Price Price,
    DateTimeOffset EffectiveFrom) : IDomainEvent
{
    public static PriceCreated Now(PriceId priceId, ProductId productId, Price price, DateTimeOffset effectiveFrom) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, priceId, productId, price, effectiveFrom);
}
