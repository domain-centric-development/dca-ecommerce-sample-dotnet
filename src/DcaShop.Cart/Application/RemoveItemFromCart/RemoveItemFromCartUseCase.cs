using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.RemoveItemFromCart;

public sealed class RemoveItemFromCartUseCase : IRemoveItemFromCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;

    public RemoveItemFromCartUseCase(IShoppingCartRepository carts, IDomainEventPublisher events)
    {
        _carts = carts;
        _events = events;
    }

    public async Task<RemoveItemFromCartResult> ExecuteAsync(RemoveItemFromCartCommand command, CancellationToken cancellationToken = default)
    {
        var cartId = new CartId(command.CartId);
        var cart = await _carts.FindByIdAsync(cartId, cancellationToken).ConfigureAwait(false)
                   ?? throw new ArgumentException($"Cart not found: {cartId}", nameof(command));

        cart.RemoveItem(new CartItemId(command.ItemId));

        await _carts.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(cart, cancellationToken).ConfigureAwait(false);

        return new RemoveItemFromCartResult(cart.Id.Value, cart.ItemCount, cart.CalculateTotal().ToString());
    }
}
