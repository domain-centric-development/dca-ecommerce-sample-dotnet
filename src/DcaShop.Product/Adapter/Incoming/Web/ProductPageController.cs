using DcaShop.Product.Application.GetAllProducts;
using DcaShop.Product.Application.GetProductById;
using DcaShop.Product.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Product.Adapter.Incoming.Web;

[Route("products")]
public sealed class ProductPageController : Controller
{
    private readonly IGetAllProductsInputPort _getAllProducts;
    private readonly IGetProductByIdInputPort _getProductById;

    public ProductPageController(IGetAllProductsInputPort getAllProducts, IGetProductByIdInputPort getProductById)
    {
        _getAllProducts = getAllProducts;
        _getProductById = getProductById;
    }

    [HttpGet("")]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken)
    {
        var result = await _getAllProducts.ExecuteAsync(new GetAllProductsQuery(), cancellationToken);
        var items = result.Products
            .Select(p => new ProductCatalogPageViewModel.Item(p.ProductId.Value, p.Name.Value, p.Sku.Value, p.Category.Name, p.ImageUrl.Value, p.CurrentPrice.ToString(), p.CanBePurchased))
            .ToList();
        return View("~/Views/Product/Catalog.cshtml", new ProductCatalogPageViewModel(items));
    }

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> Detail(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _getProductById.ExecuteAsync(new GetProductByIdQuery(productId), cancellationToken);
        if (result.Product is not { } p)
        {
            return NotFound();
        }

        return View("~/Views/Product/Detail.cshtml", ToViewModel(p));
    }

    private static ProductDetailPageViewModel ToViewModel(EnrichedProduct p) =>
        new(p.ProductId.Value, p.Name.Value, p.Sku.Value, p.Description.Value, p.Category.Name, p.ImageUrl.Value, p.CurrentPrice.ToString(), p.Article.AvailableStock, p.CanBePurchased);
}
