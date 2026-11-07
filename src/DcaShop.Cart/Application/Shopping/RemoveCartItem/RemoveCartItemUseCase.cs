using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.Shopping.RemoveCartItem;

/// <summary>
/// Takes one item out of the caller's active cart by its own id. Only the caller's active cart is asked, so an item of
/// somebody else's cart is as absent as one that was never there.
/// </summary>
/// <remarks>
/// Every refusal is returned as a case of <see cref="RemoveCartItemResult"/>, the aggregate's caught once here; each is
/// decided before anything is written, so the transaction that returns it commits nothing.
/// </remarks>
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
        return await _transactionBoundary.InTransactionAsync<RemoveCartItemResult>(
            async ct =>
            {
                var itemId = new CartItemId(command.CartItemId);
                var cart = await _carts.FindActiveByCustomerAsync(CustomerId.Of(command.CustomerId), ct).ConfigureAwait(false);
                if (cart is null)
                {
                    return new RemoveCartItemResult.CartItemNotFound(itemId);
                }

                try
                {
                    cart.RemoveItem(itemId);
                }
                catch (CartNotModifiableException notModifiable)
                {
                    return new RemoveCartItemResult.CartNotModifiable(notModifiable.CartId, notModifiable.Status);
                }
                catch (CartItemNotFoundException)
                {
                    return new RemoveCartItemResult.CartItemNotFound(itemId);
                }

                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);

                return new RemoveCartItemResult.Removed(cart.Id.Value, cart.ItemCount);
            },
            cancellationToken).ConfigureAwait(false);
    }
}