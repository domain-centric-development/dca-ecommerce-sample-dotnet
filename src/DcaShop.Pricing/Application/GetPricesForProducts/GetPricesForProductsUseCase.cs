using DcaShop.Pricing.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Pricing.Application.GetPricesForProducts;

/// <summary>Read use case: no transaction, the repository reads its own consistent snapshot.</summary>
public sealed class GetPricesForProductsUseCase : IGetPricesForProductsInputPort
{
    private readonly IProductPriceRepository _prices;

    public GetPricesForProductsUseCase(IProductPriceRepository prices)
    {
        _prices = prices;
    }

    public async Task<GetPricesForProductsResult> ExecuteAsync(GetPricesForProductsQuery input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ProductIds.Count == 0)
        {
            return new GetPricesForProductsResult(new Dictionary<ProductId, GetPricesForProductsResult.PriceData>());
        }

        var found = await _prices.FindByProductIdsAsync(input.ProductIds, cancellationToken).ConfigureAwait(false);
        var prices = found.ToDictionary(
            p => p.ProductId,
            p => new GetPricesForProductsResult.PriceData(p.ProductId, p.CurrentPrice, p.EffectiveFrom));
        return new GetPricesForProductsResult(prices);
    }
}
