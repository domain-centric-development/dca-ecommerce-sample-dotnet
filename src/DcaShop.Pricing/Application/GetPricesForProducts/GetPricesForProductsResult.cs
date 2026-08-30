using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Pricing.Application.GetPricesForProducts;

public sealed record GetPricesForProductsResult(IReadOnlyDictionary<ProductId, GetPricesForProductsResult.PriceData> Prices)
{
    public sealed record PriceData(ProductId ProductId, Money CurrentPrice, DateTimeOffset EffectiveFrom);
}
