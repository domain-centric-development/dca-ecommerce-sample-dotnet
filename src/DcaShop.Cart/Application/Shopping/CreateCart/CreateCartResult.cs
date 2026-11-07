using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Cart.Application.Shopping.CreateCart;

/// <summary>What opening a cart came to: the cart was created, or the customer already shops in another one.</summary>
[GenerateOneOf]
public sealed partial class CreateCartResult
    : OneOfBase<CreateCartResult.Created, CreateCartResult.ActiveCartAlreadyExists>
{
    /// <summary>The cart was created.</summary>
    public sealed record Created(Guid CartId, string CustomerId);

    /// <summary>
    /// The customer already has an active cart — a customer holds one at a time, and the store refuses the second.
    /// </summary>
    public sealed record ActiveCartAlreadyExists(CustomerId CustomerId) : IRefusal;
}