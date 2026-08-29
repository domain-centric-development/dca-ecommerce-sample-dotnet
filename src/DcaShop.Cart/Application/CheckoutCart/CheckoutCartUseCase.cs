using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.CheckoutCart;

public sealed class CheckoutCartUseCase : ICheckoutCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;

    public CheckoutCartUseCase(IShoppingCartRepository carts, IDomainEventPublisher events)
    {
        _carts = carts;
        _events = events;
    }

    public async Task<CheckoutCartResult> ExecuteAsync(CheckoutCartCommand command, CancellationToken cancellationToken = default)
    {
        var cartId = new CartId(command.CartId);
        var cart = await _carts.FindByIdAsync(cartId, cancellationToken).ConfigureAwait(false)
                   ?? throw new ArgumentException($"Cart not found: {cartId}", nameof(command));

        cart.Checkout();

        await _carts.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(cart, cancellationToken).ConfigureAwait(false);

        return new CheckoutCartResult(cart.Id.Value, cart.Status.ToString(), cart.CalculateTotal().ToString());
    }
}
