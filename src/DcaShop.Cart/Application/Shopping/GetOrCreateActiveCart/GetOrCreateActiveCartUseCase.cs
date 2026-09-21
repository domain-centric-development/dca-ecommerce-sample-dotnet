using DomainCentric.BuildingBlocks.Application.Transactions;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;

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
        var customerId = CustomerId.Of(command.CustomerId);

        var existing = await _carts.FindActiveByCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return new GetOrCreateActiveCartResult(existing.Id.Value, Created: false);
        }

        // The store refuses a second active cart for the same customer, so a request that lost the race takes the
        // cart that won rather than adding one of its own.
        //
        // The claim is attempted in a transaction of its own, and the recovery happens outside it. A store that
        // refuses a write mid-transaction leaves that transaction unusable — a relational one has to roll it back
        // before anything else can be read — so a catch that carried on inside it would read fine and then fail at
        // commit. The boundary is drawn here for that reason.
        try
        {
            return await _transactionBoundary.InTransactionAsync(
                async ct =>
                {
                    var cart = new ShoppingCart(CartId.Generate(), customerId);
                    await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                    await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);
                    return new GetOrCreateActiveCartResult(cart.Id.Value, Created: true);
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (ActiveCartAlreadyExistsException)
        {
            var winner = await _carts.FindActiveByCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
            if (winner is null)
            {
                throw;
            }

            return new GetOrCreateActiveCartResult(winner.Id.Value, Created: false);
        }
    }
}
