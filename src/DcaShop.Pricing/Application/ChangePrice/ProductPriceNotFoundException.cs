using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Pricing.Application.ChangePrice;

/// <summary>Raised when a price change names a product Pricing holds no price for.</summary>
public sealed class ProductPriceNotFoundException : UseCaseException
{
    public ProductPriceNotFoundException(ProductId productId)
        : base($"No price for product: {productId.Value}")
    {
        ProductId = productId;
    }

    public ProductId ProductId { get; }
}