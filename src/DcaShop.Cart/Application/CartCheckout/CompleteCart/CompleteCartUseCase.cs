using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.CartCheckout.CompleteCart;

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
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var cartId = new CartId(command.CartId);
                var cart = await _carts.FindByIdAsync(cartId, ct).ConfigureAwait(false)
                           ?? throw new CartNotFoundException(cartId);

                if (command.EventId is { } eventId && await _processedEvents.ContainsAsync(Effect, eventId, ct).ConfigureAwait(false))
                {
                    return new CompleteCartResult(cart.Id.Value, cart.Status.ToString(), AlreadyCompleted: true);
                }

                cart.ReconcileCheckout(command.SessionId, command.PurchasedPositions);

                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);
                if (command.EventId is { } processed)
                {
                    await _processedEvents.MarkProcessedAsync(Effect, processed, ct).ConfigureAwait(false);
                }

                return new CompleteCartResult(cart.Id.Value, cart.Status.ToString());
            },
            cancellationToken).ConfigureAwait(false);
    }
}