using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetActiveCart;
using DcaShop.Cart.Application.Shopping.GetCartById;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Cart.Application.Shopping.RemoveCartItem;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Application.Shared;

using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Cart.Adapter.Incoming.Web.Shopping;

/// <summary>Driving adapter for the cart page; routes and markup mirror the Java sample (<c>/cart</c>, <c>/cart/add-product</c>).</summary>
[Route("cart")]
public sealed class CartPageController : Controller
{
    private const string CartPage = "/cart";

    private readonly IGetOrCreateActiveCartInputPort _getOrCreateActiveCart;
    private readonly IGetActiveCartInputPort _getActiveCart;
    private readonly IAddItemToCartInputPort _addItemToCart;
    private readonly IRemoveCartItemInputPort _removeCartItem;
    private readonly IIdentityProvider _identityProvider;

    public CartPageController(
        IGetOrCreateActiveCartInputPort getOrCreateActiveCart,
        IGetActiveCartInputPort getActiveCart,
        IAddItemToCartInputPort addItemToCart,
        IRemoveCartItemInputPort removeCartItem,
        IIdentityProvider identityProvider)
    {
        _getOrCreateActiveCart = getOrCreateActiveCart;
        _getActiveCart = getActiveCart;
        _addItemToCart = addItemToCart;
        _removeCartItem = removeCartItem;
        _identityProvider = identityProvider;
    }

    /// <summary>The customer's active cart; a customer without one sees the empty cart — reading opens none.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Show(CancellationToken cancellationToken)
    {
        var (notice, count) = TempData.TakeCartPageNotice(CartPageNotices.CartPage);
        var result = await _getActiveCart.ExecuteAsync(new GetActiveCartQuery(CurrentCustomerId), cancellationToken);
        var viewModel = result is { Cart: { } cart, Totals: { } totals } ? ToViewModel(cart, totals) : CartPageViewModel.Empty;
        return View("~/Views/Cart/View.cshtml", WithNotice(viewModel, notice, count));
    }

    [HttpPost("add-product")]
    public async Task<IActionResult> AddProduct([FromForm] Guid productId, [FromForm] int quantity, CancellationToken cancellationToken)
    {
        var cartId = await ActiveCartIdAsync(cancellationToken);
        try
        {
            await _addItemToCart.ExecuteAsync(new AddItemToCartCommand(cartId, CurrentCustomerId, productId, quantity), cancellationToken);
            TempData.Leave(CartPageNotices.CartPage, CartPageNotice.ProductAdded);
            return Redirect(CartPage);
        }
        catch (Exception e) when (e is UseCaseException or DomainException or ArgumentException)
        {
            // The context's own refusals, plus the argument exception a form field still reaches a value object
            // with.
            if (e is InsufficientArticleStockException insufficient)
            {
                TempData.Leave(CartPageNotices.CartPage, CartPageNotice.NotEnoughStock, insufficient.Requested);
            }
            else
            {
                TempData.Leave(CartPageNotices.CartPage, NoticeFor(e));
            }

            return Redirect(CartPage);
        }
    }

    /// <summary>
    /// "Remove" on a cart item: the item goes, whatever its quantity. An id that is no item id, or names an item the
    /// visitor's cart does not hold, changes nothing — the page shows the cart as it is, without a notice.
    /// </summary>
    [HttpPost("remove-item")]
    public async Task<IActionResult> RemoveItem([FromForm] string? cartItemId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(cartItemId, out var itemId))
        {
            return Redirect(CartPage);
        }

        try
        {
            await _removeCartItem.ExecuteAsync(new RemoveCartItemCommand(CurrentCustomerId, itemId), cancellationToken);
        }
        catch (DomainException e) when (e is CartItemNotFoundException or CartNotModifiableException)
        {
            // Nothing to take out of this cart: the page it lands on already shows that.
        }

        return Redirect(CartPage);
    }

    /// <summary>
    /// What the page tells the customer, chosen from the failure's type — the exception's message is for the log,
    /// never for the page.
    /// </summary>
    private static CartPageNotice NoticeFor(Exception failure) => failure switch
    {
        ArticleNotAvailableException => CartPageNotice.ProductUnavailable,
        CartNotFoundException => CartPageNotice.CartNotFound,
        CartNotModifiableException => CartPageNotice.CartNotModifiable,
        ArgumentException => CartPageNotice.InvalidQuantity,
        _ => CartPageNotice.ProductNotAdded,
    };

    /// <summary>The page with the sentence a redirect's notice names, in the slot of its kind.</summary>
    private static CartPageViewModel WithNotice(CartPageViewModel page, CartPageNotice? notice, int? count) =>
        notice is not { } named || named.Sentence(count) is not { } sentence
            ? page
            : named.IsSuccess() ? page with { SuccessMessage = sentence } : page with { ErrorMessage = sentence };

    private string CurrentCustomerId => _identityProvider.GetCurrentIdentity().UserId.Value;

    private async Task<Guid> ActiveCartIdAsync(CancellationToken cancellationToken)
    {
        var result = await _getOrCreateActiveCart.ExecuteAsync(new GetOrCreateActiveCartCommand(CurrentCustomerId), cancellationToken);
        return result.CartId;
    }

    private static CartPageViewModel ToViewModel(EnrichedCart cart, CartTotals totals) =>
        new(
            cart.CartId.Value,
            StatusOf(cart.Status),
            cart.Items.Select(ToLine).ToList(),
            cart.ItemCount,
            cart.TotalQuantity,
            totals.CurrentSubtotal.ToString(),
            totals.ContainedTax.ToString(),
            cart.HasAnyPriceChanges,
            cart.IsValidForCheckout);

    /// <summary>The status as the cart view names it, written out per value.</summary>
    private static string StatusOf(CartStatus status) => status switch
    {
        CartStatus.Active => "Active",
        CartStatus.Completed => "Completed",
        CartStatus.Abandoned => "Abandoned",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

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