using DcaShop.Cart.Application.CheckoutCart;
using DcaShop.Cart.Application.CompleteCart;
using DcaShop.Cart.Application.GetCartById;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Cart.Api;

/// <summary>Open Host Service of the Shopping Cart: cart snapshots and lifecycle operations for other contexts (primarily Checkout).</summary>
[OpenHostService("Shopping Cart", Description = "Provides cart data and checkout operations for other bounded contexts")]
public sealed class CartService
{
    private readonly IGetCartByIdInputPort _getCartById;
    private readonly ICheckoutCartInputPort _checkoutCart;
    private readonly ICompleteCartInputPort _completeCart;

    public CartService(IGetCartByIdInputPort getCartById, ICheckoutCartInputPort checkoutCart, ICompleteCartInputPort completeCart)
    {
        _getCartById = getCartById;
        _checkoutCart = checkoutCart;
        _completeCart = completeCart;
    }

    public sealed record CartSnapshot(Guid CartId, string CustomerId, IReadOnlyList<CartItemSnapshot> Items, bool Active);

    public sealed record CartItemSnapshot(ProductId ProductId, Price PriceAtAddition, int Quantity);

    public async Task<CartSnapshot?> FindCartByIdAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        var result = await _getCartById.ExecuteAsync(new GetCartByIdQuery(cartId), cancellationToken).ConfigureAwait(false);
        if (result.Cart is not { } cart)
        {
            return null;
        }

        var items = cart.Items.Select(i => new CartItemSnapshot(i.ProductId, i.PriceAtAddition, i.Quantity.Value)).ToList();
        return new CartSnapshot(cart.CartId.Value, cart.CustomerId.Value, items, cart.Status == CartStatus.Active);
    }

    public Task MarkAsCheckedOutAsync(Guid cartId, CancellationToken cancellationToken = default) =>
        _checkoutCart.ExecuteAsync(new CheckoutCartCommand(cartId), cancellationToken);

    public Task CompleteCartAsync(Guid cartId, CancellationToken cancellationToken = default) =>
        _completeCart.ExecuteAsync(new CompleteCartCommand(cartId), cancellationToken);
}
