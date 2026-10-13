using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Cart.Application.Shopping.AddItemToCart;

/// <summary>Raised when the article facts a cart position needs are not available for a product.</summary>
/// <remarks>
/// The cart does not own the assortment; it asks for the article through its own port. A product the answer
/// does not carry cannot become a position, and that is a statement about the request, not about the cart.
/// A reference that is not even shaped like a product id is the same statement.
/// </remarks>
public sealed class ArticleNotAvailableException : UseCaseException
{
    public ArticleNotAvailableException(string productReference)
        : base($"Product not found: {productReference}")
    {
        ProductReference = productReference;
    }

    /// <summary>The product as the caller named it.</summary>
    public string ProductReference { get; }
}