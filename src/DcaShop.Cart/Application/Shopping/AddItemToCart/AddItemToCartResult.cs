using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Cart.Application.Shopping.AddItemToCart;

/// <summary>
/// What adding an item to a cart came to: the item was added, or one of the refusals a caller answers. A caller
/// translates it with <c>Match</c>, one function per case; the arity of <c>Match</c> holds that it answers every one.
/// </summary>
/// <remarks>
/// A closed result over OneOf, as every result of this context that has a refusal: C# has no closed hierarchy the
/// compiler checks for exhaustiveness yet. Once the samples run on a .NET whose C# <c>union</c> types are out of
/// preview (C# 15, .NET 11), each such result becomes a <c>union</c> of the same cases.
/// </remarks>
[GenerateOneOf]
public sealed partial class AddItemToCartResult
    : OneOfBase<
        AddItemToCartResult.Added,
        AddItemToCartResult.ArticleNotAvailable,
        AddItemToCartResult.CartNotFound,
        AddItemToCartResult.InsufficientArticleStock,
        AddItemToCartResult.CartNotModifiable>
{
    /// <summary>The item was added; the cart's figures after the addition.</summary>
    public sealed record Added(Guid CartId, int ItemCount, int TotalQuantity, Money Total);

    /// <summary>
    /// The article facts a cart position needs are not available for the product. The cart does not own the
    /// assortment; a product the answer does not carry cannot become a position, and that is a statement about the
    /// request, not about the cart.
    /// </summary>
    public sealed record ArticleNotAvailable(ProductId ProductId) : IRefusal;

    /// <summary>
    /// The addressed cart is not available to the asking customer — it does not exist or is somebody else's, told
    /// apart on purpose by nothing (ADR-007).
    /// </summary>
    public sealed record CartNotFound(CartId CartId) : IRefusal;

    /// <summary>
    /// The article facts do not cover what the cart's line would hold: the quantity the customer wants to add beside
    /// what the line holds already. The figure is a snapshot the cart read through its port, not the warehouse's own
    /// decision; counting the line keeps it from growing past the stock one addition at a time.
    /// </summary>
    /// <param name="ProductId">The product.</param>
    /// <param name="Requested">The quantity the customer asked to add.</param>
    /// <param name="Held">What the cart's line already holds of the product; 0 without a line.</param>
    /// <param name="InStock">The article's available stock as read for this request.</param>
    public sealed record InsufficientArticleStock(ProductId ProductId, int Requested, int Held, int InStock) : IRefusal;

    /// <summary>The cart is no longer active and takes no item — the aggregate's rule.</summary>
    /// <param name="CartId">The cart.</param>
    /// <param name="Status">The status that refuses the change.</param>
    public sealed record CartNotModifiable(CartId CartId, CartStatus Status) : IRefusal;
}