using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.CartCheckout.CompleteCart;

/// <summary>
/// Reconciles a cart with a confirmed checkout. The cart is loaded through its owner, the customer the confirmed
/// checkout names: a cart that does not exist and one that is somebody else's are the same refusal,
/// <see cref="CompleteCartResult.CartNotFound"/>, returned before anything is written.
/// </summary>
public sealed class CompleteCartUseCase : ICompleteCartInputPort
{
    /// <summary>The effect's key in <see cref="IProcessedEvents"/> — stable: a rename would replay old events.</summary>
    private const string Effect = "complete-cart";

    private readonly IShoppingCartRepository _carts;
    private readonly IProcessedEvents _processedEvents;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public CompleteCartUseCase(IShoppingCartRepository carts, IProcessedEvents processedEvents, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _carts = carts;
        _processedEvents = processedEvents;
        _events = events;
    }

    public async Task<CompleteCartResult> ExecuteAsync(CompleteCartCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Whole use case is local: one short transaction, the effect and its mark in it
        return await _transactionBoundary.InTransactionAsync<CompleteCartResult>(
            async ct =>
            {
                var cartId = new CartId(command.CartId);
                var cart = await _carts.FindByIdForCustomerAsync(cartId, CustomerId.Of(command.CustomerId), ct).ConfigureAwait(false);
                if (cart is null)
                {
                    return new CompleteCartResult.CartNotFound(cartId);
                }

                if (command.EventId is { } eventId && await _processedEvents.ContainsAsync(Effect, eventId, ct).ConfigureAwait(false))
                {
                    return new CompleteCartResult.Completed(cart.Id.Value, cart.Status.ToString(), AlreadyCompleted: true);
                }

                cart.ReconcileCheckout(command.SessionId, command.PurchasedPositions);

                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);
                if (command.EventId is { } processed)
                {
                    await _processedEvents.MarkProcessedAsync(Effect, processed, ct).ConfigureAwait(false);
                }

                return new CompleteCartResult.Completed(cart.Id.Value, cart.Status.ToString());
            },
            cancellationToken).ConfigureAwait(false);
    }
}