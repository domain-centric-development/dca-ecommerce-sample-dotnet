using DcaShop.Cart.Application.CheckoutCart;
using DcaShop.Cart.Application.CompleteCart;
using DcaShop.Cart.Application.GetActiveCart;
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
    private readonly IGetActiveCartInputPort _getActiveCart;
    private readonly ICheckoutCartInputPort _checkoutCart;
    private readonly ICompleteCartInputPort _completeCart;

    public CartService(IGetCartByIdInputPort getCartById, IGetActiveCartInputPort getActiveCart, ICheckoutCartInputPort checkoutCart, ICompleteCartInputPort completeCart)
    {
        _getCartById = getCartById;
        _getActiveCart = getActiveCart;
        _checkoutCart = checkoutCart;
        _completeCart = completeCart;
    }

    public sealed record CartSnapshot(Guid CartId, string CustomerId, IReadOnlyList<CartItemSnapshot> Items, bool Active);

    public sealed record CartItemSnapshot(ProductId ProductId, Price PriceAtAddition, int Quantity);

    /// <summary>What the site header needs to render the mini basket.</summary>
    public sealed record MiniBasket(Guid CartId, int ItemCount, IReadOnlyList<MiniBasketItem> Items, string Total);

    public sealed record MiniBasketItem(string Name, int Quantity);

    /// <summary>The customer's active cart as a mini basket, or null when there is none — never creates a cart.</summary>
    public async Task<MiniBasket?> FindMiniBasketAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var result = await _getActiveCart.ExecuteAsync(new GetActiveCartQuery(customerId), cancellationToken).ConfigureAwait(false);
        if (result.Cart is not { } cart)
        {
            return null;
        }

        return new MiniBasket(
            cart.CartId.Value,
            cart.TotalQuantity,
            cart.Items.Select(i => new MiniBasketItem(i.Article.Name, i.Quantity.Value)).ToList(),
            cart.CurrentSubtotal.ToString());
    }

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
