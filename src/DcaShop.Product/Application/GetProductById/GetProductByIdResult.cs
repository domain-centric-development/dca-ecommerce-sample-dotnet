using DcaShop.Product.Domain.Model;

namespace DcaShop.Product.Application.GetProductById;

/// <summary><see cref="Product"/> is null when no product with the requested id exists.</summary>
public sealed record GetProductByIdResult(EnrichedProduct? Product)
{
    public bool Found => Product is not null;
}
