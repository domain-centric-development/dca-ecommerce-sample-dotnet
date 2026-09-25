using DcaShop.Product.Application.GetProductSelection;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Product.Adapter.Incoming.Web;

/// <summary>
/// The "Discover products" slider as a page fragment: another context's page invokes it by name
/// (<c>Component.InvokeAsync("ProductSlider")</c>) without referencing this context. Renders nothing when no product
/// has a price.
/// </summary>
public sealed class ProductSliderViewComponent : ViewComponent
{
    private readonly IGetProductSelectionInputPort _getProductSelection;

    public ProductSliderViewComponent(IGetProductSelectionInputPort getProductSelection)
    {
        _getProductSelection = getProductSelection;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var result = await _getProductSelection.ExecuteAsync(new GetProductSelectionQuery(), HttpContext.RequestAborted);
        if (result.Products.Count == 0)
        {
            return Content(string.Empty);
        }

        var cards = result.Products
            .Select(p => new ProductSliderViewModel.Card(p.ProductId.Value, p.Name.Value, p.ImageUrl.Value, p.CurrentPrice.ToString()))
            .ToList();
        return View(new ProductSliderViewModel(cards));
    }
}