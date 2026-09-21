using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Cart.Application.Shopping.AddItemToCart;

/// <summary>Raised when the article facts a cart position needs are not available for a product.</summary>
/// <remarks>
/// The cart does not own the assortment; it asks for the article through its own port. A product the answer
/// does not carry cannot become a position, and that is a statement about the request, not about the cart.
/// </remarks>
public sealed class ArticleNotAvailableException : UseCaseException
{
    public ArticleNotAvailableException(ProductId productId)
        : base($"Product not found: {productId.Value}")
    {
        ProductId = productId;
    }

    public ProductId ProductId { get; }
}
