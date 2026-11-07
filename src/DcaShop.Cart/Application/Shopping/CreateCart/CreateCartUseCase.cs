using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.Shopping.CreateCart;

/// <summary>
/// Opens an empty cart for a customer. A customer holds one active cart at a time; the store states that rule and
/// refuses the second one with <see cref="ActiveCartAlreadyExistsException"/>, which this use case returns as
/// <see cref="CreateCartResult.ActiveCartAlreadyExists"/>.
/// </summary>
/// <remarks>
/// The store refuses the write mid-transaction; the case is returned from inside the boundary, which rolls a returned
/// refusal back like a thrown failure, so nothing of the refused write is committed.
/// </remarks>
public sealed class CreateCartUseCase : ICreateCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public CreateCartUseCase(IShoppingCartRepository carts, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _carts = carts;
        _events = events;
    }

    public async Task<CreateCartResult> ExecuteAsync(CreateCartCommand command, CancellationToken cancellationToken = default)
    {
        // Whole use case is local: one short transaction
        return await _transactionBoundary.InTransactionAsync<CreateCartResult>(
            async ct =>
            {
                var cart = ShoppingCart.Create(CartId.Generate(), CustomerId.Of(command.CustomerId));
                try
                {
                    await _carts.SaveAsync(cart, ct).ConfigureAwait(false);
                }
                catch (ActiveCartAlreadyExistsException alreadyActive)
                {
                    return new CreateCartResult.ActiveCartAlreadyExists(alreadyActive.CustomerId);
                }

                await _events.PublishAndClearEventsAsync(cart, ct).ConfigureAwait(false);
                return new CreateCartResult.Created(cart.Id.Value, cart.CustomerId.Value);
            },
            cancellationToken).ConfigureAwait(false);
    }
}