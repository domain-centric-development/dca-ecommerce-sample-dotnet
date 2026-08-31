using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.Inventory.Api;
using DcaShop.Pricing.Api;
using DcaShop.Product.Api;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.Extensions.Logging;

namespace DcaShop.Cart.Adapter.Outgoing.Product;

/// <summary>
/// Anti-corruption layer that assembles the cart's <see cref="CartArticle"/> from three Open Host Services:
/// identity and description from the Product Catalog, the price from Pricing, availability from Inventory.
/// </summary>
/// <remarks>
/// The only place in this context that knows those Apis — every other layer sees the cart's own article type.
/// A product nobody has priced cannot be sold: the adapter offers it as unavailable and logs a warning, rather
/// than inventing a figure or failing the request. That is also the state right after a product is created,
/// until Pricing has consumed the catalog's event.
/// </remarks>
public sealed class CompositeArticleDataAdapter : IArticleDataPort
{
    private readonly ProductCatalogService _catalog;
    private readonly PricingService _pricing;
    private readonly InventoryService _inventory;
    private readonly ILogger<CompositeArticleDataAdapter> _logger;

    public CompositeArticleDataAdapter(
        ProductCatalogService catalog,
        PricingService pricing,
        InventoryService inventory,
        ILogger<CompositeArticleDataAdapter> logger)
    {
        _catalog = catalog;
        _pricing = pricing;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<ProductId, CartArticle>> GetArticleDataAsync(
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<ProductId, CartArticle>();
        }

        var ids = productIds.Distinct().ToArray();
        var prices = await _pricing.GetPricesAsync(ids, cancellationToken).ConfigureAwait(false);
        var stocks = await _inventory.GetStockAsync(ids, cancellationToken).ConfigureAwait(false);

        var articles = new Dictionary<ProductId, CartArticle>();
        foreach (var id in ids)
        {
            var product = await _catalog.GetProductInfoAsync(id, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                continue;
            }

            articles[id] = Translate(product, prices.GetValueOrDefault(id), stocks.GetValueOrDefault(id));
        }

        return articles;
    }

    public async Task<CartArticle?> GetArticleDataAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        var articles = await GetArticleDataAsync(new[] { productId }, cancellationToken).ConfigureAwait(false);
        return articles.GetValueOrDefault(productId);
    }

    private CartArticle Translate(
        ProductCatalogService.ProductInfo product, PricingService.PriceInfo? price, InventoryService.StockInfo? stock)
    {
        if (price is null)
        {
            _logger.LogWarning(
                "No price for product {ProductId} - offering it as unavailable. Pricing may not have consumed "
                + "ProductCreatedEvent yet, or the price was never set.",
                product.ProductId.Value);
        }

        var isPriced = price is not null;
        return new CartArticle(
            product.ProductId,
            product.Name,
            price?.CurrentPrice ?? Money.Euro(0m),
            isPriced ? stock?.AvailableStock ?? 0 : 0,
            isPriced && (stock?.IsAvailable ?? false),
            product.ImageUrl);
    }
}
