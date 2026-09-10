using DcaShop.Inventory.Application.Shared;
using DcaShop.Inventory.Domain.Model;
using DcaShop.Inventory.Domain.Specification;

namespace DcaShop.Inventory.Application.GetLowStockProducts;

/// <summary>Read use case: no transaction.</summary>
public sealed class GetLowStockProductsUseCase : IGetLowStockProductsInputPort
{
    private readonly IStockLevelRepository _stockLevels;

    public GetLowStockProductsUseCase(IStockLevelRepository stockLevels)
    {
        _stockLevels = stockLevels;
    }

    public async Task<GetLowStockProductsResult> ExecuteAsync(GetLowStockProductsQuery input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var low = new AvailableQuantityBelow(StockQuantity.Of(input.Threshold));
        var matching = await _stockLevels.FindByAsync(low, cancellationToken).ConfigureAwait(false);
        var products = matching
            .Select(stockLevel => new GetLowStockProductsResult.LowStockProduct(stockLevel.ProductId, stockLevel.AvailableQuantity.Value))
            .ToList();
        return new GetLowStockProductsResult(products);
    }
}
