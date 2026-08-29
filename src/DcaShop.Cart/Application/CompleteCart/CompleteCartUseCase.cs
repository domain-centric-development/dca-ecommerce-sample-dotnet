using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.CompleteCart;

public sealed class CompleteCartUseCase : ICompleteCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;

    public CompleteCartUseCase(IShoppingCartRepository carts, IDomainEventPublisher events)
    {
        _carts = carts;
        _events = events;
    }

    public async Task<CompleteCartResult> ExecuteAsync(CompleteCartCommand command, CancellationToken cancellationToken = default)
    {
        var cartId = new CartId(command.CartId);
        var cart = await _carts.FindByIdAsync(cartId, cancellationToken).ConfigureAwait(false)
                   ?? throw new ArgumentException($"Cart not found: {cartId}", nameof(command));

        cart.Complete();

        await _carts.SaveAsync(cart, cancellationToken).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(cart, cancellationToken).ConfigureAwait(false);

        return new CompleteCartResult(cart.Id.Value, cart.Status.ToString());
    }
}
