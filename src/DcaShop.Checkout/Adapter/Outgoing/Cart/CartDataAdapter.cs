using DcaShop.Cart.Api;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Adapter.Outgoing.Cart;

/// <summary>Anti-corruption layer towards the Shopping Cart: translates the cart's published snapshot into checkout's <see cref="CartData"/>.</summary>
public sealed class CartDataAdapter : ICartDataPort
{
    private readonly CartService _cartService;

    public CartDataAdapter(CartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartData?> FindByIdAsync(CartId cartId, CustomerId customerId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _cartService.FindCartByIdAsync(cartId.Value, customerId.Value, cancellationToken).ConfigureAwait(false);
        return snapshot is null ? null : Translate(snapshot);
    }

    private static CartData Translate(CartService.CartSnapshot snapshot) =>
        new(
            new CartId(snapshot.CartId),
            CustomerId.Of(snapshot.CustomerId),
            snapshot.Items.Select(i => new CartData.CartItemData(i.ProductId, i.PriceAtAddition.Value, i.Quantity)).ToList(),
            snapshot.Active);
}
