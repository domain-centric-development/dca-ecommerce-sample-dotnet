using DcaShop.Pricing.Application.GetPricesForProducts;
using DcaShop.Pricing.Application.SetProductPrice;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Pricing.Api;

/// <summary>Open Host Service of Pricing: what a product costs, for other bounded contexts.</summary>
[OpenHostService("Pricing", Description = "Provides pricing information for other bounded contexts")]
public sealed class PricingService
{
    private readonly IGetPricesForProductsInputPort _getPrices;
    private readonly ISetProductPriceInputPort _setPrice;

    public PricingService(IGetPricesForProductsInputPort getPrices, ISetProductPriceInputPort setPrice)
    {
        _getPrices = getPrices;
        _setPrice = setPrice;
    }

    public sealed record PriceInfo(ProductId ProductId, Money CurrentPrice, DateTimeOffset EffectiveFrom);

    public async Task<IReadOnlyDictionary<ProductId, PriceInfo>> GetPricesAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<ProductId, PriceInfo>();
        }

        var result = await _getPrices.ExecuteAsync(new GetPricesForProductsQuery(productIds), cancellationToken).ConfigureAwait(false);
        return result.Prices.ToDictionary(
            entry => entry.Key,
            entry => new PriceInfo(entry.Value.ProductId, entry.Value.CurrentPrice, entry.Value.EffectiveFrom));
    }

    public async Task<PriceInfo?> GetPriceAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        var prices = await GetPricesAsync(new[] { productId }, cancellationToken).ConfigureAwait(false);
        return prices.TryGetValue(productId, out var price) ? price : null;
    }

    public Task SetPriceAsync(ProductId productId, Money price, CancellationToken cancellationToken = default) =>
        _setPrice.ExecuteAsync(new SetProductPriceCommand(productId.Value, price.Amount, price.Currency), cancellationToken);
}
