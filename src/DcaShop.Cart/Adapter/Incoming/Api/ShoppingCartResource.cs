using DcaShop.Cart.Application.Operations.GetAllCarts;
using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.CreateCart;
using DcaShop.Cart.Application.Shopping.GetCartById;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Cart.Application.Shopping.RemoveItemFromCart;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Authorization;
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
    private readonly ICreateCartInputPort _createCart;
    private readonly IGetOrCreateActiveCartInputPort _getOrCreateActiveCart;
    private readonly IGetAllCartsInputPort _getAllCarts;
    private readonly IGetCartByIdInputPort _getCartById;
    private readonly IAddItemToCartInputPort _addItemToCart;
    private readonly IRemoveItemFromCartInputPort _removeItemFromCart;
    private readonly ShoppingCartDtoConverter _converter;
    private readonly IIdentityProvider _identityProvider;

    public ShoppingCartResource(
        ICreateCartInputPort createCart,
        IGetOrCreateActiveCartInputPort getOrCreateActiveCart,
        IGetAllCartsInputPort getAllCarts,
        IGetCartByIdInputPort getCartById,
        IAddItemToCartInputPort addItemToCart,
        IRemoveItemFromCartInputPort removeItemFromCart,
        ShoppingCartDtoConverter converter,
        IIdentityProvider identityProvider)
    {
        _createCart = createCart;
        _getOrCreateActiveCart = getOrCreateActiveCart;
        _getAllCarts = getAllCarts;
        _getCartById = getCartById;
        _addItemToCart = addItemToCart;
        _removeItemFromCart = removeItemFromCart;
        _converter = converter;
        _identityProvider = identityProvider;
    }

    /// <summary>
    /// Opens the caller's active cart. A caller who already has one is refused — the store's
    /// <see cref="Application.Shared.ActiveCartAlreadyExistsException"/>, answered by <see cref="CartApiExceptionHandler"/>.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ShoppingCartDto>> CreateCart(CancellationToken cancellationToken)
    {
        var result = await _createCart.ExecuteAsync(new CreateCartCommand(CurrentCustomerId), cancellationToken);
        var cart = await ReadAsync(result.CartId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, _converter.ToDto(cart!));
    }

    /// <summary>Every cart in the shop — the operator view, and the only route that leaves the caller's own data.</summary>
    [HttpGet]
    [Authorize(Roles = Role.StaffCode)]
    public async Task<ActionResult<ShoppingCartListDto>> GetAllCarts(CancellationToken cancellationToken)
    {
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

        // A product id that does not even parse is answered here, at the edge, as the product nobody carries.
        if (!ProductId.TryOf(request.ProductId, out var productId))
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Article not available",
                detail: $"There is no article for product {request.ProductId}.");
        }

        // Each refusal travels as its own type; CartApiExceptionHandler decides what the caller is told.
        await _addItemToCart.ExecuteAsync(
            new AddItemToCartCommand(cartId, CurrentCustomerId, productId.Value, request.Quantity), cancellationToken);

        return Ok(_converter.ToDto((await ReadAsync(cartId, cancellationToken))!));
    }

    /// <summary>Removes the item holding the product named in the path, whatever its quantity.</summary>
    [HttpDelete("{cartId:guid}/items/{productId:guid}")]
    public async Task<ActionResult<ShoppingCartDto>> RemoveItemFromCart(
        Guid cartId, Guid productId, CancellationToken cancellationToken)
    {
        if (await ReadOwnAsync(cartId, cancellationToken) is null)
        {
            return NotFound();
        }

        await _removeItemFromCart.ExecuteAsync(new RemoveItemFromCartCommand(cartId, CurrentCustomerId, productId), cancellationToken);

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