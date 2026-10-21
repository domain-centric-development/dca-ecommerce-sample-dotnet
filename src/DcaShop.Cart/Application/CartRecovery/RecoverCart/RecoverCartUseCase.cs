using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.CartRecovery.RecoverCart;

/// <summary>
/// Carries a guest's cart over to the account at login, for the case where there is nothing to decide: the
/// account's cart holds nothing, so the guest cart itself — its id and its positions — becomes the account's.
/// </summary>
/// <remarks>
/// It is the other half of <see cref="MergeCarts.MergeCartsUseCase"/>: when both carts hold items the visitor is
/// asked which one to keep, and when only the guest cart does, there is no question to ask — but the items still
/// have to follow the identity, or logging in would silently empty the cart. The use case is idempotent: with
/// matching identities, or with no guest cart, it does nothing.
/// </remarks>
public sealed class RecoverCartUseCase : IRecoverCartInputPort
{
    private readonly IShoppingCartRepository _carts;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public RecoverCartUseCase(
        IShoppingCartRepository carts, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _carts = carts;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public async Task<RecoverCartResult> ExecuteAsync(
        RecoverCartCommand command, CancellationToken cancellationToken = default)
    {
        var anonymousCustomerId = CustomerId.Of(command.AnonymousUserId);
        var registeredCustomerId = CustomerId.Of(command.RegisteredUserId);

        if (command.GuestBeforeLogin != command.AnonymousUserId)
        {
            throw new GuestCartNotOwnedException(anonymousCustomerId);
        }

        if (anonymousCustomerId == registeredCustomerId)
        {
            return RecoverCartResult.NoRecoveryNeeded(registeredCustomerId.Value);
        }

        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var anonymousCart = await _carts.FindActiveByCustomerAsync(anonymousCustomerId, ct)
                    .ConfigureAwait(false);

                if (anonymousCart is null || anonymousCart.IsEmpty)
                {
                    return RecoverCartResult.NoRecoveryNeeded(registeredCustomerId.Value);
                }

                var accountCart = await _carts.FindActiveByCustomerAsync(registeredCustomerId, ct)
                    .ConfigureAwait(false);

                if (accountCart is null || accountCart.IsEmpty)
                {
                    return await HandOverAsync(anonymousCart, accountCart, registeredCustomerId, ct).ConfigureAwait(false);
                }

                var itemsRecovered = 0;
                foreach (var item in anonymousCart.Items)
                {
                    accountCart.AddItem(item.ProductId, item.Quantity, item.PriceAtAddition);
                    itemsRecovered++;
                }

                await _carts.SaveAsync(accountCart, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(accountCart, ct).ConfigureAwait(false);
                await _carts.DeleteByIdAsync(anonymousCart.Id, ct).ConfigureAwait(false);

                return Recovered(accountCart, itemsRecovered, anonymousCartDeleted: true);
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The guest cart itself becomes the account's — same id, same positions — so a checkout captured from it is
    /// still the checkout of the account's cart. An empty cart the account had goes, there is one active cart each.
    /// </summary>
    private async Task<RecoverCartResult> HandOverAsync(
        ShoppingCart anonymousCart, ShoppingCart? emptyAccountCart, CustomerId registeredCustomerId, CancellationToken ct)
    {
        if (emptyAccountCart is not null)
        {
            await _carts.DeleteByIdAsync(emptyAccountCart.Id, ct).ConfigureAwait(false);
        }

        anonymousCart.HandOverTo(registeredCustomerId);
        await _carts.SaveAsync(anonymousCart, ct).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(anonymousCart, ct).ConfigureAwait(false);

        return Recovered(anonymousCart, anonymousCart.ItemCount, anonymousCartDeleted: false);
    }

    /// <summary>The account's cart after the recovery, whichever way the guest's positions reached it.</summary>
    private static RecoverCartResult Recovered(ShoppingCart cart, int itemsRecovered, bool anonymousCartDeleted) =>
        new(
            cart.Id.Value,
            cart.CustomerId.Value,
            cart.Items
                .Select(i => new RecoverCartResult.CartItemSummary(
                    i.Id.Value, i.ProductId.Value, i.Quantity.Value, i.PriceAtAddition.Value))
                .ToList(),
            cart.CalculateTotal(),
            itemsRecovered,
            anonymousCartDeleted);
}