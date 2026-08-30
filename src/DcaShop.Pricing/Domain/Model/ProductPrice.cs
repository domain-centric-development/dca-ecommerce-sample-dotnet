using DcaShop.Pricing.Domain.Event;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Pricing.Domain.Model;

/// <summary>
/// The current price of one product, with the moment it took effect. Aggregate root of the Pricing context:
/// a price is always greater than zero, and every change is recorded as a domain event.
/// </summary>
public sealed class ProductPrice : AggregateRootBase<ProductPrice, PriceId>
{
    private ProductPrice(PriceId id, ProductId productId, Money currentPrice, DateTimeOffset effectiveFrom)
    {
        Id = id;
        ProductId = productId;
        CurrentPrice = currentPrice;
        EffectiveFrom = effectiveFrom;
    }

    public override PriceId Id { get; }

    public ProductId ProductId { get; }

    public Money CurrentPrice { get; private set; }

    public DateTimeOffset EffectiveFrom { get; private set; }

    /// <summary>Creates the first price of a product and registers <see cref="PriceCreated"/>.</summary>
    public static ProductPrice Create(ProductId productId, Money price)
    {
        ValidateGreaterThanZero(price);
        var effectiveFrom = DateTimeOffset.UtcNow;
        var productPrice = new ProductPrice(PriceId.Generate(), productId, price, effectiveFrom);
        productPrice.RegisterEvent(PriceCreated.Now(productPrice.Id, productId, price, effectiveFrom));
        return productPrice;
    }

    /// <summary>Sets a new price and registers <see cref="PriceChanged"/> with the previous one.</summary>
    public void UpdatePrice(Money newPrice)
    {
        ValidateGreaterThanZero(newPrice);
        var oldPrice = CurrentPrice;
        CurrentPrice = newPrice;
        EffectiveFrom = DateTimeOffset.UtcNow;
        RegisterEvent(PriceChanged.Now(Id, ProductId, oldPrice, newPrice, EffectiveFrom));
    }

    private static void ValidateGreaterThanZero(Money price)
    {
        ArgumentNullException.ThrowIfNull(price);
        if (price.Amount <= 0m)
        {
            throw new ArgumentException("Price must be greater than zero", nameof(price));
        }
    }
}
