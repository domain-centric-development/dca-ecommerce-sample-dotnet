using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.RemoveItemFromCart;

public sealed class RemoveItemFromCartUseCase : IRemoveItemFromCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveItemFromCartUseCase(IShoppingCartRepository carts, IDomainEventPublisher events, IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _carts = carts;
        _events = events;
    }

    public async Task<RemoveItemFromCartResult> ExecuteAsync(RemoveItemFromCartCommand command, CancellationToken cancellationToken = default)
    {
        // Whole use case is local: one short unit of work
        return await _unitOfWork.RunAsync(
            async ct =>
            {
                var cartId = new CartId(command.CartId);
                var cart = await _carts.FindByIdAsync(cartId, ct).ConfigureAwait(false)
                           ?? throw new ArgumentException($"Cart not found: {cartId}", nameof(command));

                cart.RemoveItem(new CartItemId(command.ItemId));

                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);

                return new RemoveItemFromCartResult(cart.Id.Value, cart.ItemCount, cart.CalculateTotal().ToString());
            },
            cancellationToken).ConfigureAwait(false);
    }
}
