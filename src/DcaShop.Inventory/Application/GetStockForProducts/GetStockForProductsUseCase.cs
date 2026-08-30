using DcaShop.Inventory.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Inventory.Application.GetStockForProducts;

/// <summary>Read use case: no transaction.</summary>
public sealed class GetStockForProductsUseCase : IGetStockForProductsInputPort
{
    private readonly IStockLevelRepository _stockLevels;

    public GetStockForProductsUseCase(IStockLevelRepository stockLevels)
    {
        _stockLevels = stockLevels;
    }

    public async Task<GetStockForProductsResult> ExecuteAsync(GetStockForProductsQuery input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.ProductIds.Count == 0)
        {
            return new GetStockForProductsResult(new Dictionary<ProductId, GetStockForProductsResult.StockData>());
        }

        var found = await _stockLevels.FindByProductIdsAsync(input.ProductIds, cancellationToken).ConfigureAwait(false);
        var stocks = found.ToDictionary(
            s => s.ProductId,
            s => new GetStockForProductsResult.StockData(s.ProductId, s.AvailableQuantity.Value, s.IsAvailable));
        return new GetStockForProductsResult(stocks);
    }
}
