using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.Shopping.RemoveCartItem;

/// <summary>
/// Takes one item out of the caller's active cart by its own id. Only the caller's active cart is asked, so an item of
/// somebody else's cart is as absent as one that was never there.
/// </summary>
public sealed class RemoveCartItemUseCase : IRemoveCartItemInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public RemoveCartItemUseCase(IShoppingCartRepository carts, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _carts = carts;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public async Task<RemoveCartItemResult> ExecuteAsync(RemoveCartItemCommand command, CancellationToken cancellationToken = default)
    {
        // Whole use case is local: one short transaction
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var itemId = new CartItemId(command.CartItemId);
                var cart = await _carts.FindActiveByCustomerAsync(CustomerId.Of(command.CustomerId), ct).ConfigureAwait(false)
                           ?? throw CartItemNotFoundException.ForItem(itemId);

                cart.RemoveItem(itemId);

                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);

                return new RemoveCartItemResult(cart.Id.Value, cart.ItemCount);
            },
            cancellationToken).ConfigureAwait(false);
    }
}