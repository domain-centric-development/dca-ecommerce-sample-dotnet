using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Inventory.Application.GetLowStockProducts;

public sealed record GetLowStockProductsResult(IReadOnlyList<GetLowStockProductsResult.LowStockProduct> Products)
{
    public sealed record LowStockProduct(ProductId ProductId, int AvailableQuantity);
}
