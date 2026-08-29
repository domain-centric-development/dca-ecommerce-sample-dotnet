using DcaShop.Product.Application.GetAllProducts;
using DcaShop.Product.Application.GetProductById;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Product.Api;

/// <summary>
/// Open Host Service of the Product Catalog. Provides product identity and description to other contexts.
/// <see cref="ProductArticleInfo"/> also carries the current price and stock: until the Pricing and Inventory
/// contexts exist in this sample, the catalog relays what its own ports answer.
/// </summary>
[OpenHostService("Product Catalog", Description = "Product identity, description and — for now — article price and stock for other bounded contexts")]
public sealed class ProductCatalogService
{
    private readonly IGetProductByIdInputPort _getProductById;
    private readonly IGetAllProductsInputPort _getAllProducts;

    public ProductCatalogService(IGetProductByIdInputPort getProductById, IGetAllProductsInputPort getAllProducts)
    {
        _getProductById = getProductById;
        _getAllProducts = getAllProducts;
    }

    public sealed record ProductInfo(ProductId ProductId, string Name, string Sku, string ImageUrl);

    public sealed record ProductArticleInfo(ProductId ProductId, string Name, string Sku, string ImageUrl, Money CurrentPrice, int AvailableStock, bool IsAvailable);

    public async Task<ProductInfo?> GetProductInfoAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        var result = await _getProductById.ExecuteAsync(new GetProductByIdQuery(productId.Value), cancellationToken).ConfigureAwait(false);
        return result.Product is null ? null : ToInfo(result.Product);
    }

    public async Task<ProductArticleInfo?> GetProductArticleAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        var result = await _getProductById.ExecuteAsync(new GetProductByIdQuery(productId.Value), cancellationToken).ConfigureAwait(false);
        return result.Product is null ? null : ToArticleInfo(result.Product);
    }

    public async Task<IReadOnlyDictionary<ProductId, ProductArticleInfo>> GetProductArticlesAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<ProductId, ProductArticleInfo>();
        foreach (var id in productIds.Distinct())
        {
            var article = await GetProductArticleAsync(id, cancellationToken).ConfigureAwait(false);
            if (article is not null)
            {
                result[id] = article;
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<ProductInfo>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _getAllProducts.ExecuteAsync(new GetAllProductsQuery(), cancellationToken).ConfigureAwait(false);
        return result.Products.Select(ToInfo).ToList();
    }

    private static ProductInfo ToInfo(EnrichedProduct p) => new(p.ProductId, p.Name.Value, p.Sku.Value, p.ImageUrl.Value);

    private static ProductArticleInfo ToArticleInfo(EnrichedProduct p) =>
        new(p.ProductId, p.Name.Value, p.Sku.Value, p.ImageUrl.Value, p.Article.CurrentPrice, p.Article.AvailableStock, p.Article.IsAvailable);
}
