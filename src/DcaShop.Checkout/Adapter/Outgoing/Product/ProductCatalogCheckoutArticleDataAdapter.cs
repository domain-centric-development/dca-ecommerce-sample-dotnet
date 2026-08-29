using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Product.Api;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Checkout.Adapter.Outgoing.Product;

/// <summary>Anti-corruption layer towards the Product Catalog: translates its published article info into <see cref="CheckoutArticle"/>.</summary>
public sealed class ProductCatalogCheckoutArticleDataAdapter : ICheckoutArticleDataPort
{
    private readonly ProductCatalogService _catalog;

    public ProductCatalogCheckoutArticleDataAdapter(ProductCatalogService catalog)
    {
        _catalog = catalog;
    }

    public async Task<IReadOnlyDictionary<ProductId, CheckoutArticle>> GetArticleDataAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        var articles = await _catalog.GetProductArticlesAsync(productIds, cancellationToken).ConfigureAwait(false);
        return articles.ToDictionary(
            e => e.Key,
            e => new CheckoutArticle(e.Value.ProductId, e.Value.Name, e.Value.CurrentPrice, e.Value.IsAvailable, e.Value.AvailableStock, e.Value.ImageUrl));
    }
}
