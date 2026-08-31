using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Cart.Application.CheckoutCart;

/// <summary>
/// Adapts freshly read article data to the domain's <see cref="IArticlePriceResolver"/>. A product the catalog
/// did not answer for counts as unavailable — the aggregate then says so line by line.
/// </summary>
internal sealed class ArticleDataPriceResolver : IArticlePriceResolver
{
    private readonly IReadOnlyDictionary<ProductId, CartArticle> _articles;

    internal ArticleDataPriceResolver(IReadOnlyDictionary<ProductId, CartArticle> articles)
    {
        _articles = articles;
    }

    public ArticlePrice Resolve(ProductId productId) =>
        _articles.TryGetValue(productId, out var article)
            ? new ArticlePrice(article.CurrentPrice, article.IsAvailable, article.AvailableStock)
            : new ArticlePrice(Money.Euro(0m), false, 0);
}
