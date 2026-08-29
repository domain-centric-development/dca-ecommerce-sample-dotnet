using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Product.Application.Shared;

/// <summary>Combines a product with the price and stock answers of the outgoing ports into an <see cref="EnrichedProduct"/>.</summary>
public sealed class ProductArticleAssembler
{
    private const string DefaultCurrency = "EUR";

    private readonly IPricingDataPort _pricing;
    private readonly IProductStockDataPort _stock;

    public ProductArticleAssembler(IPricingDataPort pricing, IProductStockDataPort stock)
    {
        _pricing = pricing;
        _stock = stock;
    }

    public async Task<IReadOnlyList<EnrichedProduct>> EnrichAsync(IReadOnlyList<Domain.Model.Product> products, CancellationToken cancellationToken)
    {
        if (products.Count == 0)
        {
            return Array.Empty<EnrichedProduct>();
        }

        var ids = products.Select(p => p.Id).ToArray();
        var prices = await _pricing.GetPricesAsync(ids, cancellationToken).ConfigureAwait(false);
        var stocks = await _stock.GetStockDataAsync(ids, cancellationToken).ConfigureAwait(false);
        return products.Select(p => EnrichedProduct.From(p, ArticleOf(p.Id, prices, stocks))).ToList();
    }

    private static ProductArticle ArticleOf(ProductId id, IReadOnlyDictionary<ProductId, PriceData> prices, IReadOnlyDictionary<ProductId, StockData> stocks)
    {
        var price = prices.TryGetValue(id, out var p) ? p.CurrentPrice : Money.Zero(DefaultCurrency);
        var hasStock = stocks.TryGetValue(id, out var s);
        return new ProductArticle(price, hasStock ? s!.AvailableStock : 0, hasStock && s!.IsAvailable);
    }
}
