using System.ComponentModel;
using DcaShop.Product.Adapter.Incoming.Api;
using DcaShop.Product.Application.GetAllProducts;
using DcaShop.Product.Application.GetProductById;
using ModelContextProtocol.Server;

namespace DcaShop.Product.Adapter.Incoming.Mcp;

/// <summary>
/// The product catalog as MCP tools, so an AI assistant can read the assortment. A driving adapter like any
/// other: a different protocol in front of the very same input ports the web pages and the REST API use.
/// </summary>
/// <remarks>
/// Read-only on purpose. The tools return the same <see cref="ProductDto"/> the REST API does, through the same
/// converter — one representation of a product, not two that can drift. Exposed at <c>/mcp</c>, which is
/// Bearer-only like the rest of the token surface (ADR-007).
/// </remarks>
[McpServerToolType]
public sealed class ProductCatalogMcpToolProvider
{
    private readonly IGetAllProductsInputPort _getAllProducts;
    private readonly IGetProductByIdInputPort _getProductById;
    private readonly ProductDtoConverter _converter;

    public ProductCatalogMcpToolProvider(
        IGetAllProductsInputPort getAllProducts,
        IGetProductByIdInputPort getProductById,
        ProductDtoConverter converter)
    {
        _getAllProducts = getAllProducts;
        _getProductById = getProductById;
        _converter = converter;
    }

    [McpServerTool(Name = "all-products")]
    [Description("Get all products in the catalog. Returns complete product information including SKU, name, price, category, and available stock.")]
    public async Task<IReadOnlyList<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _getAllProducts.ExecuteAsync(new GetAllProductsQuery(), cancellationToken);
        return result.Products.Select(_converter.ToDto).ToList();
    }

    [McpServerTool(Name = "product-by-id")]
    [Description("Get detailed product information by product ID. Requires the internal product UUID. Returns complete product details including all attributes.")]
    public async Task<ProductDto?> GetProductByIdAsync(
        [Description("The product ID (UUID format)")] Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _getProductById.ExecuteAsync(new GetProductByIdQuery(id), cancellationToken);
        return result.Product is { } product ? _converter.ToDto(product) : null;
    }
}
