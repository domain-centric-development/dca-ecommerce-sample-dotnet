using DcaShop.Cart.Application.AddItemToCart;
using DcaShop.Cart.Application.CheckoutCart;
using DcaShop.Cart.Application.GetAllCarts;
using DcaShop.Cart.Application.GetCartById;
using DcaShop.Cart.Application.GetOrCreateActiveCart;
using DcaShop.Cart.Application.RemoveItemFromCart;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Application.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Cart.Adapter.Incoming.Api;

/// <summary>
/// The shopping cart over HTTP. Every route acts on the cart of the caller's own identity: the cart id in the
/// path selects which of the caller's carts is meant, it never selects whose cart is meant.
/// </summary>
/// <remarks>
/// <para>
/// A cart belonging to somebody else answers <c>404</c>, not <c>403</c>. A <c>403</c> would confirm that the id
/// exists, which is exactly the fact a stranger must not learn.
/// </para>
/// <para>
/// Listing every cart in the shop crosses that boundary by design and therefore demands the staff role.
/// Authenticated by an <c>Authorization: Bearer</c> token and nothing else (ADR-007).
/// </para>
/// </remarks>
[ApiController]
[Route("api/carts")]
public sealed class ShoppingCartResource : ControllerBase
{
    private readonly IGetOrCreateActiveCartInputPort _getOrCreateActiveCart;
    private readonly IGetAllCartsInputPort _getAllCarts;
    private readonly IGetCartByIdInputPort _getCartById;
    private readonly IAddItemToCartInputPort _addItemToCart;
    private readonly IRemoveItemFromCartInputPort _removeItemFromCart;
    private readonly ICheckoutCartInputPort _checkoutCart;
    private readonly ShoppingCartDtoConverter _converter;
    private readonly IIdentityProvider _identityProvider;

    public ShoppingCartResource(
        IGetOrCreateActiveCartInputPort getOrCreateActiveCart,
        IGetAllCartsInputPort getAllCarts,
        IGetCartByIdInputPort getCartById,
        IAddItemToCartInputPort addItemToCart,
        IRemoveItemFromCartInputPort removeItemFromCart,
        ICheckoutCartInputPort checkoutCart,
        ShoppingCartDtoConverter converter,
        IIdentityProvider identityProvider)
    {
        _getOrCreateActiveCart = getOrCreateActiveCart;
        _getAllCarts = getAllCarts;
        _getCartById = getCartById;
        _addItemToCart = addItemToCart;
        _removeItemFromCart = removeItemFromCart;
        _checkoutCart = checkoutCart;
        _converter = converter;
        _identityProvider = identityProvider;
    }

    /// <summary>Creates the caller's active cart, or returns the one they already have.</summary>
    [HttpPost]
    public async Task<ActionResult<ShoppingCartDto>> CreateCart(CancellationToken cancellationToken)
    {
        var result = await _getOrCreateActiveCart.ExecuteAsync(
            new GetOrCreateActiveCartCommand(CurrentCustomerId), cancellationToken);
        var cart = await ReadAsync(result.CartId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, _converter.ToDto(cart!));
    }

    /// <summary>Every cart in the shop — the operator view, and the only route that leaves the caller's own data.</summary>
    [HttpGet]
    public async Task<ActionResult<ShoppingCartListDto>> GetAllCarts(CancellationToken cancellationToken)
    {
        if (!_identityProvider.GetCurrentIdentity().HasRole(IIdentityProvider.IIdentity.RoleStaff))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var result = await _getAllCarts.ExecuteAsync(new GetAllCartsQuery(), cancellationToken);
        return Ok(_converter.ToListDto(result));
    }

    [HttpGet("{cartId:guid}")]
    public async Task<ActionResult<ShoppingCartDto>> GetCart(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await ReadOwnAsync(cartId, cancellationToken);
        return cart is null ? NotFound() : Ok(_converter.ToDto(cart));
    }

    /// <summary>
    /// The caller's active cart. The customer id in the path is the Java sample's route shape; it must name the
    /// caller, and any other value is treated as a cart that does not exist for them.
    /// </summary>
    [HttpGet("customer/{customerId}/active")]
    public async Task<ActionResult<ShoppingCartDto>> GetOrCreateActiveCart(
        string customerId, CancellationToken cancellationToken)
    {
        if (!string.Equals(customerId, CurrentCustomerId, StringComparison.Ordinal))
        {
            return NotFound();
        }

        var result = await _getOrCreateActiveCart.ExecuteAsync(
            new GetOrCreateActiveCartCommand(CurrentCustomerId), cancellationToken);
        var cart = await ReadAsync(result.CartId, cancellationToken);
        return Ok(_converter.ToDto(cart!));
    }

    [HttpPost("{cartId:guid}/items")]
    public async Task<ActionResult<ShoppingCartDto>> AddItemToCart(
        Guid cartId, [FromBody] AddToCartRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await ReadOwnAsync(cartId, cancellationToken) is null)
        {
            return NotFound();
        }

        try
        {
            await _addItemToCart.ExecuteAsync(
                new AddItemToCartCommand(cartId, CurrentCustomerId, request.ProductId, request.Quantity), cancellationToken);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            return BadRequest(e.Message);
        }

        return Ok(_converter.ToDto((await ReadAsync(cartId, cancellationToken))!));
    }

    /// <summary>
    /// Removes one line. The path names the <b>cart item</b>, not the product: the use case removes a line by its
    /// own id, and the Java sample's product-keyed route is the shape that would have to change here.
    /// </summary>
    [HttpDelete("{cartId:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<ShoppingCartDto>> RemoveItemFromCart(
        Guid cartId, Guid itemId, CancellationToken cancellationToken)
    {
        if (await ReadOwnAsync(cartId, cancellationToken) is null)
        {
            return NotFound();
        }

        try
        {
            await _removeItemFromCart.ExecuteAsync(new RemoveItemFromCartCommand(cartId, CurrentCustomerId, itemId), cancellationToken);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            return BadRequest(e.Message);
        }

        return Ok(_converter.ToDto((await ReadAsync(cartId, cancellationToken))!));
    }

    [HttpPost("{cartId:guid}/checkout")]
    public async Task<ActionResult<ShoppingCartDto>> Checkout(Guid cartId, CancellationToken cancellationToken)
    {
        if (await ReadOwnAsync(cartId, cancellationToken) is null)
        {
            return NotFound();
        }

        try
        {
            await _checkoutCart.ExecuteAsync(new CheckoutCartCommand(cartId, CurrentCustomerId), cancellationToken);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            return BadRequest(e.Message);
        }

        return Ok(_converter.ToDto((await ReadAsync(cartId, cancellationToken))!));
    }

    private string CurrentCustomerId => _identityProvider.GetCurrentIdentity().UserId.Value;

    private async Task<EnrichedCart?> ReadAsync(Guid cartId, CancellationToken cancellationToken) =>
        (await _getCartById.ExecuteAsync(new GetCartByIdQuery(cartId, CurrentCustomerId), cancellationToken)).Cart;

    /// <summary>The cart, but only if it is the caller's — otherwise null, and the caller is told it is not there.</summary>
    private async Task<EnrichedCart?> ReadOwnAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await ReadAsync(cartId, cancellationToken);
        return cart is not null && cart.CustomerId.Value == CurrentCustomerId ? cart : null;
    }
}
