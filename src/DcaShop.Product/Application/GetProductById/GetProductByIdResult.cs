using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Product.Application.GetProductById;

/// <summary>What looking a product up came to: found, or no product with the requested id exists.</summary>
[GenerateOneOf]
public sealed partial class GetProductByIdResult : OneOfBase<GetProductByIdResult.Found, GetProductByIdResult.ProductNotFound>
{
    /// <summary>The product, with the article facts it is shown with.</summary>
    public sealed record Found(EnrichedProduct Product);

    /// <summary>No product with the requested id exists.</summary>
    public sealed record ProductNotFound(ProductId ProductId) : IRefusal;
}