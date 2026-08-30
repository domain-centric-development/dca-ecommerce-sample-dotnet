using DcaShop.Product.Application.CreateProduct;
using DcaShop.Product.Application.GetAllProducts;
using DcaShop.Product.Application.GetProductById;
using DcaShop.SharedKernel.Application.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Product.Adapter.Incoming.Api;

/// <summary>
/// The product catalog over HTTP. Reading it is public — it is the same assortment the shop pages show — while
/// creating a product is an operator action and demands the staff role.
/// </summary>
/// <remarks>
/// Authenticated by an <c>Authorization: Bearer</c> token and nothing else: no cookie of this browser reaches
/// this adapter, which is what lets the whole <c>/api/</c> surface skip the antiforgery token (ADR-007).
/// </remarks>
[ApiController]
[Route("api/products")]
public sealed class ProductResource : ControllerBase
{
    private readonly ICreateProductInputPort _createProduct;
    private readonly IGetAllProductsInputPort _getAllProducts;
    private readonly IGetProductByIdInputPort _getProductById;
    private readonly ProductDtoConverter _converter;
    private readonly IIdentityProvider _identityProvider;

    public ProductResource(
        ICreateProductInputPort createProduct,
        IGetAllProductsInputPort getAllProducts,
        IGetProductByIdInputPort getProductById,
        ProductDtoConverter converter,
        IIdentityProvider identityProvider)
    {
        _createProduct = createProduct;
        _getAllProducts = getAllProducts;
        _getProductById = getProductById;
        _converter = converter;
        _identityProvider = identityProvider;
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_identityProvider.GetCurrentIdentity().HasRole(IIdentityProvider.IIdentity.RoleStaff))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var command = new CreateProductCommand(
            request.Sku,
            request.Name,
            request.Description ?? string.Empty,
            request.ImageUrl ?? string.Empty,
            request.Price,
            "EUR",
            request.Category,
            request.Stock);

        try
        {
            var result = await _createProduct.ExecuteAsync(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, _converter.ToDto(result, command));
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            // A malformed SKU or a duplicate one is the caller's mistake, not the server's.
            return BadRequest(e.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAllProducts(CancellationToken cancellationToken)
    {
        var result = await _getAllProducts.ExecuteAsync(new GetAllProductsQuery(), cancellationToken);
        return Ok(result.Products.Select(_converter.ToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getProductById.ExecuteAsync(new GetProductByIdQuery(id), cancellationToken);
        return result.Product is { } product ? Ok(_converter.ToDto(product)) : NotFound();
    }
}
