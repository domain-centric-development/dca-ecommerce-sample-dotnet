using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.CheckoutCompletion.ResumeCheckout;

/// <summary>
/// Finds the open checkout of the customer's active cart and hands it to the customer — the session belongs to the
/// cart, so when a guest's cart became the account's at a login, the guest's checkout follows it. Never starts a
/// session: a cart without an open checkout leaves nothing to resume.
/// </summary>
public sealed class ResumeCheckoutUseCase : IResumeCheckoutInputPort
{
    private readonly ICartDataPort _cartData;
    private readonly ICheckoutSessionRepository _sessions;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public ResumeCheckoutUseCase(ICartDataPort cartData, ICheckoutSessionRepository sessions, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _cartData = cartData;
        _sessions = sessions;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public async Task<ResumeCheckoutResult> ExecuteAsync(ResumeCheckoutCommand command, CancellationToken cancellationToken = default)
    {
        var customerId = CustomerId.Of(command.CustomerId);
        // The cart comes from another context (remote-capable) — outside the transaction
        var cart = await _cartData.FindActiveForCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (cart is null)
        {
            return ResumeCheckoutResult.NothingToResume();
        }

        // Serialized with starting and confirming a checkout of the same cart
        return await _sessions.InCartSessionAsync(cart.CartId, () => _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var session = await _sessions.FindActiveByCartIdAsync(cart.CartId, ct).ConfigureAwait(false);
                if (session is null)
                {
                    return ResumeCheckoutResult.NothingToResume();
                }

                if (session.CustomerId != customerId)
                {
                    session.HandOverTo(customerId);
                    await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                    await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                }

                return ResumeCheckoutResult.From(session);
            },
            cancellationToken), cancellationToken).ConfigureAwait(false);
    }
}