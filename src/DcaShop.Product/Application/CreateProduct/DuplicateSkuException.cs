using DcaShop.Product.Domain.Model;
using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Product.Application.CreateProduct;

/// <summary>Raised when a product is created with a stock keeping unit the catalog already carries.</summary>
/// <remarks>
/// Uniqueness is not an invariant a single product can hold — it is a statement about the whole catalog,
/// checked against the repository — so the failure belongs to the use case, not to the aggregate. A malformed
/// stock keeping unit is a different failure: the value object refuses it as an argument, and the caller has
/// to send something else, not something new.
/// </remarks>
public sealed class DuplicateSkuException : UseCaseException
{
    public DuplicateSkuException(Sku sku)
        : base($"Product with SKU already exists: {sku.Value}")
    {
        Sku = sku;
    }

    public Sku Sku { get; }
}
