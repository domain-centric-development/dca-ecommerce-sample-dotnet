using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Cart.Application.Shopping.AddItemToCart;

/// <summary>
/// Raised when the article facts do not cover what a line would hold once the quantity a customer wants is added to
/// what the cart already holds of the product.
/// </summary>
/// <remarks>
/// The figure is a snapshot the cart read through its port, not the warehouse's own decision — the stock
/// keeping unit refuses a shipment it cannot cover with its own rule. This check makes the refusal visible
/// while the customer is still shopping, and counting the line keeps it from growing past the stock one
/// addition at a time.
/// </remarks>
public sealed class InsufficientArticleStockException : UseCaseException
{
    public InsufficientArticleStockException(ProductId productId, int requested, int held, int inStock)
        : base($"Insufficient stock for product {productId.Value}: {requested} requested, {held} in the cart, {inStock} in stock")
    {
        ProductId = productId;
        Requested = requested;
        Held = held;
        InStock = inStock;
    }

    public ProductId ProductId { get; }

    /// <summary>The quantity the customer asked to add.</summary>
    public int Requested { get; }

    /// <summary>What the cart's line already holds of the product; 0 without a line.</summary>
    public int Held { get; }

    /// <summary>The article's available stock as read for this request.</summary>
    public int InStock { get; }
}