using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Inventory.Application.GetStockForProducts;

public sealed record GetStockForProductsQuery(IReadOnlyCollection<ProductId> ProductIds);
