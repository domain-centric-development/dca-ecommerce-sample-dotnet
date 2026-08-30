using DomainCentric.BuildingBlocks.Application.Transactions;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.CompleteCart;

public sealed class CompleteCartUseCase : ICompleteCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public CompleteCartUseCase(IShoppingCartRepository carts, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _carts = carts;
        _events = events;
    }

    public async Task<CompleteCartResult> ExecuteAsync(CompleteCartCommand command, CancellationToken cancellationToken = default)
    {
        // Whole use case is local: one short transaction
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var cartId = new CartId(command.CartId);
                var cart = await _carts.FindByIdAsync(cartId, ct).ConfigureAwait(false)
                           ?? throw new ArgumentException($"Cart not found: {cartId}", nameof(command));

                if (cart.Status == CartStatus.Completed)
                {
                    // Idempotent: the completion trigger is delivered at least once.
                    return new CompleteCartResult(cart.Id.Value, cart.Status.ToString());
                }

                cart.Complete();

                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);

                return new CompleteCartResult(cart.Id.Value, cart.Status.ToString());
            },
            cancellationToken).ConfigureAwait(false);
    }
}
