using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetCartById;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Application.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Cart.Adapter.Incoming.Web.Shopping;

/// <summary>Driving adapter for the cart page; routes and markup mirror the Java sample (<c>/cart</c>, <c>/cart/add-product</c>).</summary>
[Route("cart")]
public sealed class CartPageController : Controller
{
    private readonly IGetOrCreateActiveCartInputPort _getOrCreateActiveCart;
    private readonly IGetCartByIdInputPort _getCartById;
    private readonly IAddItemToCartInputPort _addItemToCart;
    private readonly IIdentityProvider _identityProvider;

    public CartPageController(
        IGetOrCreateActiveCartInputPort getOrCreateActiveCart,
        IGetCartByIdInputPort getCartById,
        IAddItemToCartInputPort addItemToCart,
        IIdentityProvider identityProvider)
    {
        _getOrCreateActiveCart = getOrCreateActiveCart;
        _getCartById = getCartById;
        _addItemToCart = addItemToCart;
        _identityProvider = identityProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Show(CancellationToken cancellationToken)
    {
        var cartId = await ActiveCartIdAsync(cancellationToken);
        var result = await _getCartById.ExecuteAsync(new GetCartByIdQuery(cartId, CurrentCustomerId), cancellationToken);
        return result is { Cart: { } cart, Totals: { } totals } ? View("~/Views/Cart/View.cshtml", ToViewModel(cart, totals)) : NotFound();
    }

    [HttpPost("add-product")]
    public async Task<IActionResult> AddProduct([FromForm] Guid productId, [FromForm] int quantity, CancellationToken cancellationToken)
    {
        var cartId = await ActiveCartIdAsync(cancellationToken);
        try
        {
            await _addItemToCart.ExecuteAsync(new AddItemToCartCommand(cartId, CurrentCustomerId, productId, quantity), cancellationToken);
            TempData["Message"] = "Product added to cart!";
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = e.Message;
        }

        return RedirectToAction(nameof(Show));
    }

    private string CurrentCustomerId => _identityProvider.GetCurrentIdentity().UserId.Value;

    private async Task<Guid> ActiveCartIdAsync(CancellationToken cancellationToken)
    {
        var result = await _getOrCreateActiveCart.ExecuteAsync(new GetOrCreateActiveCartCommand(CurrentCustomerId), cancellationToken);
        return result.CartId;
    }

    private static CartPageViewModel ToViewModel(EnrichedCart cart, CartTotals totals) =>
        new(
            cart.CartId.Value,
            cart.Status.ToString(),
            cart.Items.Select(ToLine).ToList(),
            cart.ItemCount,
            cart.TotalQuantity,
            totals.CurrentSubtotal.ToString(),
            totals.ContainedTax.ToString(),
            cart.HasAnyPriceChanges,
            cart.IsValidForCheckout);

    private static CartPageViewModel.Line ToLine(EnrichedCartItem i) =>
        new(
            i.Id.Value,
            i.ProductId.Value,
            i.Article.Name,
            i.Article.ImageUrl,
            i.Quantity.Value,
            i.Article.CurrentPrice.ToString(),
            i.CurrentLineTotal.ToString(),
            i.HasPriceChanged,
            i.PriceIncreased,
            i.PriceDifference.ToString(),
            i.Article.IsAvailable,
            i.HasSufficientStock);
}
