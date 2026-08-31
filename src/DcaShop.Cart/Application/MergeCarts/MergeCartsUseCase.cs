using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.MergeCarts;

/// <summary>
/// Applies the merge strategy the visitor chose. Whatever they choose, the anonymous cart is gone afterwards:
/// its identity has been superseded by the account's, so leaving it behind would strand items nobody can reach.
/// </summary>
public sealed class MergeCartsUseCase : IMergeCartsInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public MergeCartsUseCase(
        IShoppingCartRepository carts, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _carts = carts;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public async Task<MergeCartsResult> ExecuteAsync(
        MergeCartsCommand command, CancellationToken cancellationToken = default)
    {
        var anonymousCustomerId = CustomerId.Of(command.AnonymousUserId);
        var registeredCustomerId = CustomerId.Of(command.RegisteredUserId);

        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var anonymousCart = await _carts.FindActiveByCustomerAsync(anonymousCustomerId, ct)
                    .ConfigureAwait(false);

                var accountCart = await _carts.FindActiveByCustomerAsync(registeredCustomerId, ct)
                    .ConfigureAwait(false)
                    ?? await _carts.SaveAsync(new ShoppingCart(CartId.Generate(), registeredCustomerId), ct)
                        .ConfigureAwait(false);

                var itemsFromAccount = accountCart.ItemCount;
                var itemsFromAnonymous = 0;

                if (anonymousCart is not null)
                {
                    switch (command.Strategy)
                    {
                        case CartMergeStrategy.MergeBoth:
                            itemsFromAnonymous = accountCart.Merge(anonymousCart);
                            break;

                        case CartMergeStrategy.UseAnonymousCart:
                            itemsFromAccount = 0;
                            if (!accountCart.IsEmpty)
                            {
                                accountCart.Clear();
                            }

                            itemsFromAnonymous = accountCart.Merge(anonymousCart);
                            break;

                        case CartMergeStrategy.UseAccountCart:
                        default:
                            break;
                    }

                    await _carts.SaveAsync(accountCart, ct).ConfigureAwait(false);
                    await _events.PublishAndClearEventsAsync(accountCart, ct).ConfigureAwait(false);
                    await _carts.DeleteByIdAsync(anonymousCart.Id, ct).ConfigureAwait(false);
                }

                return Summarize(accountCart, command.Strategy, itemsFromAnonymous, itemsFromAccount);
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static MergeCartsResult Summarize(
        ShoppingCart cart, CartMergeStrategy strategy, int itemsFromAnonymous, int itemsFromAccount) =>
        new(
            cart.Id.Value,
            cart.CustomerId.Value,
            cart.Items
                .Select(i => new MergeCartsResult.CartItemSummary(
                    i.Id.Value, i.ProductId.Value, i.Quantity.Value, i.PriceAtAddition.Value.ToString()))
                .ToList(),
            cart.CalculateTotal().ToString(),
            strategy,
            itemsFromAnonymous,
            itemsFromAccount,
            AnonymousCartDeleted: true);
}
