using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Product.Application.CreateProduct;

/// <summary>What creating a product came to: created, or the catalog already carries the stock keeping unit.</summary>
[GenerateOneOf]
public sealed partial class CreateProductResult : OneOfBase<CreateProductResult.Created, CreateProductResult.DuplicateSku>
{
    /// <summary>The product was created; <see cref="InitialPrice"/> is the price it was created with, as <see cref="Money"/> normalised it.</summary>
    public sealed record Created(Guid ProductId, string Sku, string Name, Money InitialPrice);

    /// <summary>
    /// The catalog already carries the stock keeping unit. Uniqueness is no invariant a single product can hold — it
    /// is a statement about the whole catalog, checked against the repository — so the refusal is the use case's. A
    /// malformed stock keeping unit is a different failure: the value object refuses it as an argument.
    /// </summary>
    public sealed record DuplicateSku(Sku Sku) : IRefusal;
}