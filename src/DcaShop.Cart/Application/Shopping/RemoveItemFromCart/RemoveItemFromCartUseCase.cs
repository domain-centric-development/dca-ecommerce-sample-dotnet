using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.Shopping.RemoveItemFromCart;

/// <summary>Removes the item holding a product from one of the caller's carts, whatever its quantity.</summary>
/// <remarks>
/// Every refusal is returned as a case of <see cref="RemoveItemFromCartResult"/>: a cart the caller does not hold, and
/// the aggregate's refusals, caught once here. Each is decided before anything is written, so the transaction that
/// returns it commits nothing.
/// </remarks>
public sealed class RemoveItemFromCartUseCase : IRemoveItemFromCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public RemoveItemFromCartUseCase(IShoppingCartRepository carts, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _carts = carts;
        _events = events;
    }

    public async Task<RemoveItemFromCartResult> ExecuteAsync(RemoveItemFromCartCommand command, CancellationToken cancellationToken = default)
    {
        // Whole use case is local: one short transaction
        return await _transactionBoundary.InTransactionAsync<RemoveItemFromCartResult>(
            async ct =>
            {
                var cartId = new CartId(command.CartId);
                var productId = new ProductId(command.ProductId);
                var cart = await _carts.FindByIdForCustomerAsync(cartId, CustomerId.Of(command.CustomerId), ct).ConfigureAwait(false);
                if (cart is null)
                {
                    return new RemoveItemFromCartResult.CartNotFound(cartId);
                }

                try
                {
                    cart.RemoveItemByProductId(productId);
                }
                catch (CartNotModifiableException notModifiable)
                {
                    return new RemoveItemFromCartResult.CartNotModifiable(notModifiable.CartId, notModifiable.Status);
                }
                catch (CartItemNotFoundException)
                {
                    return new RemoveItemFromCartResult.CartItemNotFound(productId);
                }

                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);

                return new RemoveItemFromCartResult.Removed(cart.Id.Value, cart.ItemCount, cart.CalculateTotal());
            },
            cancellationToken).ConfigureAwait(false);
    }
}