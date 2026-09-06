using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.RecoverCartOnLogin;

/// <summary>
/// Carries a guest's cart over to the account at login, for the case where there is nothing to decide: the
/// account holds no cart of its own, so the guest cart simply becomes the account's.
/// </summary>
/// <remarks>
/// It is the other half of <see cref="MergeCarts.MergeCartsUseCase"/>: when both carts hold items the visitor is
/// asked which one to keep, and when only the guest cart does, there is no question to ask — but the items still
/// have to follow the identity, or logging in would silently empty the cart. The use case is idempotent: with
/// matching identities, or with no guest cart, it does nothing.
/// </remarks>
public sealed class RecoverCartOnLoginUseCase : IRecoverCartOnLoginInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public RecoverCartOnLoginUseCase(
        IShoppingCartRepository carts, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _carts = carts;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public async Task<RecoverCartOnLoginResult> ExecuteAsync(
        RecoverCartOnLoginCommand command, CancellationToken cancellationToken = default)
    {
        var anonymousCustomerId = CustomerId.Of(command.AnonymousUserId);
        var registeredCustomerId = CustomerId.Of(command.RegisteredUserId);

        if (anonymousCustomerId == registeredCustomerId)
        {
            return RecoverCartOnLoginResult.NoRecoveryNeeded(registeredCustomerId.Value);
        }

        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var anonymousCart = await _carts.FindActiveByCustomerAsync(anonymousCustomerId, ct)
                    .ConfigureAwait(false);

                if (anonymousCart is null || anonymousCart.IsEmpty)
                {
                    return RecoverCartOnLoginResult.NoRecoveryNeeded(registeredCustomerId.Value);
                }

                var accountCart = await _carts.FindActiveByCustomerAsync(registeredCustomerId, ct)
                    .ConfigureAwait(false)
                    ?? await _carts.SaveAsync(new ShoppingCart(CartId.Generate(), registeredCustomerId), ct)
                        .ConfigureAwait(false);

                var itemsRecovered = 0;
                foreach (var item in anonymousCart.Items)
                {
                    accountCart.AddItem(item.ProductId, item.Quantity, item.PriceAtAddition);
                    itemsRecovered++;
                }

                await _carts.SaveAsync(accountCart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(accountCart, ct).ConfigureAwait(false);
                await _carts.DeleteByIdAsync(anonymousCart.Id, ct).ConfigureAwait(false);

                return new RecoverCartOnLoginResult(
                    accountCart.Id.Value,
                    accountCart.CustomerId.Value,
                    accountCart.Items
                        .Select(i => new RecoverCartOnLoginResult.CartItemSummary(
                            i.Id.Value, i.ProductId.Value, i.Quantity.Value, i.PriceAtAddition.Value.ToString()))
                        .ToList(),
                    accountCart.CalculateTotal().ToString(),
                    itemsRecovered,
                    AnonymousCartDeleted: true);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
