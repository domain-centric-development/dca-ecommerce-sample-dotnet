using DcaShop.Product.Domain.Model;

namespace DcaShop.Product.Application.GetAllProducts;

public sealed record GetAllProductsResult(IReadOnlyList<EnrichedProduct> Products);
