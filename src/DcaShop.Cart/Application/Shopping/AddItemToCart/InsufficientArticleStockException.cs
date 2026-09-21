using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Cart.Application.Shopping.AddItemToCart;

/// <summary>Raised when the article facts do not cover the quantity a customer wants to put in the cart.</summary>
/// <remarks>
/// The figure is a snapshot the cart read through its port, not the warehouse's own decision — the stock
/// keeping unit refuses a shipment it cannot cover with its own rule. This check makes the refusal visible
/// while the customer is still shopping.
/// </remarks>
public sealed class InsufficientArticleStockException : UseCaseException
{
    public InsufficientArticleStockException(ProductId productId, int requested)
        : base($"Insufficient stock for product: {productId.Value}")
    {
        ProductId = productId;
        Requested = requested;
    }

    public ProductId ProductId { get; }

    public int Requested { get; }
}
