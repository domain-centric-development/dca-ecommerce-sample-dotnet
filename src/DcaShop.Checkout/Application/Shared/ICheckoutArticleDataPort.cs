using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Current article data (name, price, availability, stock) for the products in a checkout.</summary>
public interface ICheckoutArticleDataPort : IOutputPort
{
    Task<IReadOnlyDictionary<ProductId, CheckoutArticle>> GetArticleDataAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// The article facts of this moment for the products of <paramref name="lines"/> — price, availability and stock, as
    /// the session and the availability check read them. The one translation from article data to facts: the review's
    /// fingerprint and Place Order's are computed from it alike.
    /// </summary>
    async Task<IReadOnlyDictionary<ProductId, ArticlePrice>> GetArticleFactsAsync(IEnumerable<CheckoutLineItem> lines, CancellationToken cancellationToken = default)
    {
        var articles = await GetArticleDataAsync(lines.Select(line => line.ProductId).ToArray(), cancellationToken).ConfigureAwait(false);
        return articles.ToDictionary(e => e.Key, e => new ArticlePrice(e.Value.CurrentPrice, e.Value.IsAvailable, e.Value.AvailableStock));
    }
}