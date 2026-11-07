using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Cart.Application.Shopping.GetCartById;

/// <summary>What reading one of the caller's carts came to: the cart, or no cart of theirs under that id.</summary>
[GenerateOneOf]
public sealed partial class GetCartByIdResult : OneOfBase<GetCartByIdResult.Found, GetCartByIdResult.CartNotFound>
{
    /// <summary>
    /// The cart, at today's article data. <see cref="Totals"/> carries the cart's amounts — business facts the use
    /// case assembles so that no page has to compute them.
    /// </summary>
    public sealed record Found(EnrichedCart Cart, CartTotals Totals);

    /// <summary>
    /// No cart of this customer under that id — it does not exist or is somebody else's, told apart on purpose by
    /// nothing (ADR-007).
    /// </summary>
    public sealed record CartNotFound(CartId CartId) : IRefusal;
}