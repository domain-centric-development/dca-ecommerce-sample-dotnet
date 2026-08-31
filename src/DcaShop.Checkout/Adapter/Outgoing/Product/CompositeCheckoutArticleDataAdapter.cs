using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Inventory.Api;
using DcaShop.Pricing.Api;
using DcaShop.Product.Api;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.Extensions.Logging;

namespace DcaShop.Checkout.Adapter.Outgoing.Product;

/// <summary>
/// Anti-corruption layer that assembles checkout's <see cref="CheckoutArticle"/> from three Open Host Services:
/// identity and description from the Product Catalog, the price from Pricing, availability from Inventory.
/// </summary>
/// <remarks>
/// A product nobody has priced cannot be sold: the adapter offers it as unavailable and logs a warning, rather
/// than settling on a figure nobody set. The checkout's own validation then names the line.
/// </remarks>
public sealed class CompositeCheckoutArticleDataAdapter : ICheckoutArticleDataPort
{
    private readonly ProductCatalogService _catalog;
    private readonly PricingService _pricing;
    private readonly InventoryService _inventory;
    private readonly ILogger<CompositeCheckoutArticleDataAdapter> _logger;

    public CompositeCheckoutArticleDataAdapter(
        ProductCatalogService catalog,
        PricingService pricing,
        InventoryService inventory,
        ILogger<CompositeCheckoutArticleDataAdapter> logger)
    {
        _catalog = catalog;
        _pricing = pricing;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<ProductId, CheckoutArticle>> GetArticleDataAsync(
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<ProductId, CheckoutArticle>();
        }

        var ids = productIds.Distinct().ToArray();
        var prices = await _pricing.GetPricesAsync(ids, cancellationToken).ConfigureAwait(false);
        var stocks = await _inventory.GetStockAsync(ids, cancellationToken).ConfigureAwait(false);

        var articles = new Dictionary<ProductId, CheckoutArticle>();
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

    private CheckoutArticle Translate(
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
        return new CheckoutArticle(
            product.ProductId,
            product.Name,
            price?.CurrentPrice ?? Money.Euro(0m),
            isPriced && (stock?.IsAvailable ?? false),
            isPriced ? stock?.AvailableStock ?? 0 : 0,
            product.ImageUrl);
    }
}
