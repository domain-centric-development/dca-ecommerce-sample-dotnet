using DcaShop.Product.Domain.Model;

namespace DcaShop.Product.Application.GetProductSelection;

public sealed record GetProductSelectionResult(IReadOnlyList<EnrichedProduct> Products);