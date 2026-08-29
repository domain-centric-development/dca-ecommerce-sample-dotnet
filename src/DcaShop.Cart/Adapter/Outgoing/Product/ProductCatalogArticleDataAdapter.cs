using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.Product.Api;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Cart.Adapter.Outgoing.Product;

/// <summary>
/// Anti-corruption layer towards the Product Catalog: translates its published <see cref="ProductCatalogService.ProductArticleInfo"/>
/// into the cart's own <see cref="CartArticle"/>. The only place in this context that knows the catalog's Api.
/// </summary>
public sealed class ProductCatalogArticleDataAdapter : IArticleDataPort
{
    private readonly ProductCatalogService _catalog;

    public ProductCatalogArticleDataAdapter(ProductCatalogService catalog)
    {
        _catalog = catalog;
    }

    public async Task<IReadOnlyDictionary<ProductId, CartArticle>> GetArticleDataAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        var articles = await _catalog.GetProductArticlesAsync(productIds, cancellationToken).ConfigureAwait(false);
        return articles.ToDictionary(e => e.Key, e => Translate(e.Value));
    }

    public async Task<CartArticle?> GetArticleDataAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        var article = await _catalog.GetProductArticleAsync(productId, cancellationToken).ConfigureAwait(false);
        return article is null ? null : Translate(article);
    }

    private static CartArticle Translate(ProductCatalogService.ProductArticleInfo info) =>
        new(info.ProductId, info.Name, info.CurrentPrice, info.AvailableStock, info.IsAvailable, info.ImageUrl);
}
