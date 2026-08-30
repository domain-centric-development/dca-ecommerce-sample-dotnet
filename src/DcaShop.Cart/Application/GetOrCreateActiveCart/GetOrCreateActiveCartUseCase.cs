using DomainCentric.BuildingBlocks.Application.Transactions;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.GetOrCreateActiveCart;

public sealed class GetOrCreateActiveCartUseCase : IGetOrCreateActiveCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public GetOrCreateActiveCartUseCase(IShoppingCartRepository carts, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _carts = carts;
        _events = events;
    }

    public async Task<GetOrCreateActiveCartResult> ExecuteAsync(GetOrCreateActiveCartCommand command, CancellationToken cancellationToken = default)
    {
        // Whole use case is local: one short transaction
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var customerId = CustomerId.Of(command.CustomerId);
                var existing = await _carts.FindActiveByCustomerAsync(customerId, ct).ConfigureAwait(false);
                if (existing is not null)
                {
                    return new GetOrCreateActiveCartResult(existing.Id.Value, Created: false);
                }

                var cart = new ShoppingCart(CartId.Generate(), customerId);
                await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);
                return new GetOrCreateActiveCartResult(cart.Id.Value, Created: true);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
