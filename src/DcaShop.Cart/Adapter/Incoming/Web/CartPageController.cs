using DcaShop.Cart.Application.AddItemToCart;
using DcaShop.Cart.Application.GetCartById;
using DcaShop.Cart.Application.GetOrCreateActiveCart;
using DcaShop.Cart.Application.RemoveItemFromCart;
using DcaShop.Cart.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Cart.Adapter.Incoming.Web;

[Route("cart")]
public sealed class CartPageController : Controller
{
    private readonly IGetOrCreateActiveCartInputPort _getOrCreateActiveCart;
    private readonly IGetCartByIdInputPort _getCartById;
    private readonly IAddItemToCartInputPort _addItemToCart;
    private readonly IRemoveItemFromCartInputPort _removeItemFromCart;

    public CartPageController(
        IGetOrCreateActiveCartInputPort getOrCreateActiveCart,
        IGetCartByIdInputPort getCartById,
        IAddItemToCartInputPort addItemToCart,
        IRemoveItemFromCartInputPort removeItemFromCart)
    {
        _getOrCreateActiveCart = getOrCreateActiveCart;
        _getCartById = getCartById;
        _addItemToCart = addItemToCart;
        _removeItemFromCart = removeItemFromCart;
    }

    [HttpGet("")]
    public async Task<IActionResult> Show(CancellationToken cancellationToken)
    {
        var cartId = await ActiveCartIdAsync(cancellationToken);
        var result = await _getCartById.ExecuteAsync(new GetCartByIdQuery(cartId), cancellationToken);
        return result.Cart is { } cart ? View("~/Views/Cart/Cart.cshtml", ToViewModel(cart)) : NotFound();
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromForm] Guid productId, [FromForm] int quantity, CancellationToken cancellationToken)
    {
        var cartId = await ActiveCartIdAsync(cancellationToken);
        try
        {
            await _addItemToCart.ExecuteAsync(new AddItemToCartCommand(cartId, productId, quantity), cancellationToken);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = e.Message;
        }

        return RedirectToAction(nameof(Show));
    }

    [HttpPost("items/{itemId:guid}/remove")]
    public async Task<IActionResult> RemoveItem(Guid itemId, CancellationToken cancellationToken)
    {
        var cartId = await ActiveCartIdAsync(cancellationToken);
        await _removeItemFromCart.ExecuteAsync(new RemoveItemFromCartCommand(cartId, itemId), cancellationToken);
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
            cart.Items.Select(i => new CartPageViewModel.Line(
                i.Id.Value,
                i.ProductId.Value,
                i.Article.Name,
                i.Article.ImageUrl,
                i.Quantity.Value,
                i.Article.CurrentPrice.ToString(),
                i.CurrentLineTotal.ToString(),
                i.HasPriceChanged,
                i.HasSufficientStock)).ToList(),
            cart.CurrentSubtotal.ToString(),
            cart.HasAnyPriceChanges,
            cart.IsValidForCheckout);
}
