using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Adapts a fetched article map to the domain's <see cref="ICheckoutArticlePriceResolver"/>.</summary>
public sealed class ArticleDataPriceResolver : ICheckoutArticlePriceResolver
{
    private readonly IReadOnlyDictionary<ProductId, CheckoutArticle> _articles;

    public ArticleDataPriceResolver(IReadOnlyDictionary<ProductId, CheckoutArticle> articles)
    {
        _articles = articles;
    }

    public ArticlePrice Resolve(ProductId productId)
    {
        if (!_articles.TryGetValue(productId, out var article))
        {
            throw new ArgumentException($"Article data not found for: {productId}", nameof(productId));
        }

        return new ArticlePrice(article.CurrentPrice, article.IsAvailable, article.AvailableStock);
    }
}
