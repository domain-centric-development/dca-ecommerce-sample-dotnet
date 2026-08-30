using DcaShop.Cart.Application.AddItemToCart;
using DcaShop.Cart.Application.GetCartById;
using DcaShop.Cart.Application.GetOrCreateActiveCart;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Cart.Adapter.Incoming.Web;

/// <summary>Driving adapter for the cart page; routes and markup mirror the Java sample (<c>/cart</c>, <c>/cart/add-product</c>).</summary>
[Route("cart")]
public sealed class CartPageController : Controller
{
    private readonly IGetOrCreateActiveCartInputPort _getOrCreateActiveCart;
    private readonly IGetCartByIdInputPort _getCartById;
    private readonly IAddItemToCartInputPort _addItemToCart;

    public CartPageController(
        IGetOrCreateActiveCartInputPort getOrCreateActiveCart,
        IGetCartByIdInputPort getCartById,
        IAddItemToCartInputPort addItemToCart)
    {
        _getOrCreateActiveCart = getOrCreateActiveCart;
        _getCartById = getCartById;
        _addItemToCart = addItemToCart;
    }

    [HttpGet("")]
    public async Task<IActionResult> Show(CancellationToken cancellationToken)
    {
        var cartId = await ActiveCartIdAsync(cancellationToken);
        var result = await _getCartById.ExecuteAsync(new GetCartByIdQuery(cartId), cancellationToken);
        return result.Cart is { } cart ? View("~/Views/Cart/View.cshtml", ToViewModel(cart)) : NotFound();
    }

    [HttpPost("add-product")]
    public async Task<IActionResult> AddProduct([FromForm] Guid productId, [FromForm] int quantity, CancellationToken cancellationToken)
    {
        var cartId = await ActiveCartIdAsync(cancellationToken);
        try
        {
            await _addItemToCart.ExecuteAsync(new AddItemToCartCommand(cartId, productId, quantity), cancellationToken);
            TempData["Message"] = "Product added to cart!";
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = e.Message;
        }

        return RedirectToAction(nameof(Show));
    }

    private async Task<Guid> ActiveCartIdAsync(CancellationToken cancellationToken)
    {
        var customerId = GuestCustomer.IdentifyOrCreate(HttpContext);
        var result = await _getOrCreateActiveCart.ExecuteAsync(new GetOrCreateActiveCartCommand(customerId), cancellationToken);
        return result.CartId;
    }

    private static CartPageViewModel ToViewModel(EnrichedCart cart) =>
        new(
            cart.CartId.Value,
            cart.Status.ToString(),
            cart.Items.Select(ToLine).ToList(),
            cart.ItemCount,
            cart.TotalQuantity,
            cart.CurrentSubtotal.ToString(),
            cart.HasAnyPriceChanges,
            cart.IsValidForCheckout);

    private static CartPageViewModel.Line ToLine(EnrichedCartItem i)
    {
        var current = i.Article.CurrentPrice;
        var original = i.PriceAtAddition.Value;
        var difference = Money.Of(Math.Abs(current.Amount - original.Amount), current.Currency);
        return new CartPageViewModel.Line(
            i.Id.Value,
            i.ProductId.Value,
            i.Article.Name,
            i.Article.ImageUrl,
            i.Quantity.Value,
            current.ToString(),
            i.CurrentLineTotal.ToString(),
            i.HasPriceChanged,
            current.Amount > original.Amount,
            difference.ToString(),
            i.Article.IsAvailable,
            i.HasSufficientStock);
    }
}
