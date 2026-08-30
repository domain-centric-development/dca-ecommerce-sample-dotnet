using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Pricing.Application.GetPricesForProducts;

public sealed record GetPricesForProductsQuery(IReadOnlyCollection<ProductId> ProductIds);
