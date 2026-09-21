using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when the article facts a line item needs are not available for a product.</summary>
/// <remarks>
/// Checkout asks the assortment for name, price and availability of everything in the cart. A product the
/// answer does not carry cannot become a line item, and the totals would otherwise be computed over a gap.
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
