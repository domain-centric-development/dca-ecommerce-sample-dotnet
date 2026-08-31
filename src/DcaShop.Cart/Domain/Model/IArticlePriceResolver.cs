using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Cart.Domain.Model;

/// <summary>
/// Resolves the current price and availability of an article.
/// </summary>
/// <remarks>
/// The aggregate asks for figures it must not fetch itself: the use case builds the resolver from freshly read
/// article data and passes it in, so the domain stays free of ports and infrastructure.
/// </remarks>
public interface IArticlePriceResolver
{
    /// <summary>The current price and availability of the given product.</summary>
    ArticlePrice Resolve(ProductId productId);
}
