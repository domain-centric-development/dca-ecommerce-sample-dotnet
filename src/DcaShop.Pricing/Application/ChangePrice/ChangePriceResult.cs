using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Pricing.Application.ChangePrice;

/// <summary>What a price change came to: the price changed, or Pricing holds no price for the product.</summary>
[GenerateOneOf]
public sealed partial class ChangePriceResult : OneOfBase<ChangePriceResult.Changed, ChangePriceResult.ProductPriceNotFound>
{
    /// <summary>The price a product has after the change, and the moment it took effect.</summary>
    public sealed record Changed(Guid ProductId, decimal PriceAmount, string PriceCurrency, DateTimeOffset EffectiveFrom);

    /// <summary>The change names a product Pricing holds no price for. Nothing was changed.</summary>
    public sealed record ProductPriceNotFound(ProductId ProductId) : IRefusal;
}