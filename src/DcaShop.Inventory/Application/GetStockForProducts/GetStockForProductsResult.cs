using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Inventory.Application.GetStockForProducts;

public sealed record GetStockForProductsResult(IReadOnlyDictionary<ProductId, GetStockForProductsResult.StockData> Stocks)
{
    public sealed record StockData(ProductId ProductId, int AvailableStock, bool IsAvailable);
}
