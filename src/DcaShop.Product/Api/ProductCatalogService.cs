using DcaShop.Product.Application.GetAllProducts;
using DcaShop.Product.Application.GetProductById;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Product.Api;

/// <summary>
/// Open Host Service of the Product Catalog: product identity and description for other contexts.
/// </summary>
/// <remarks>
/// Price and stock are not here. They are Pricing's and Inventory's statements, and a context that needs them
/// asks those contexts — the catalog reads them too, but only to present its own pages.
/// </remarks>
[OpenHostService("Product Catalog", Description = "Product identity and description for other bounded contexts")]
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

    public async Task<ProductInfo?> GetProductInfoAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        var result = await _getProductById.ExecuteAsync(new GetProductByIdQuery(productId.Value), cancellationToken).ConfigureAwait(false);
        return result.Product is null ? null : ToInfo(result.Product);
    }

    public async Task<IReadOnlyList<ProductInfo>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _getAllProducts.ExecuteAsync(new GetAllProductsQuery(), cancellationToken).ConfigureAwait(false);
        return result.Products.Select(ToInfo).ToList();
    }

    private static ProductInfo ToInfo(EnrichedProduct p) => new(p.ProductId, p.Name.Value, p.Sku.Value, p.ImageUrl.Value);
}
