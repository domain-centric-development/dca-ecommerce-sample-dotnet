using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Microsoft.Extensions.Logging;

namespace DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;

/// <summary>
/// Places the order the review showed: checks every line against the facts of this moment and the order at today's
/// prices against the fingerprint the review showed, reserves the order's stock in Inventory, has the payment provider
/// authorize the total the review showed and only then confirms the session at those prices. Of two orders placed at
/// once, the one whose reservation the stock no longer covers is refused, naming the line, requests no payment and
/// changes nothing; a refused or unanswered payment gives the reservation back and places no order.
/// </summary>
/// <remarks>
/// Every refusal is returned as a case of <see cref="ConfirmCheckoutResult"/>, the session's own caught once here. A
/// refusal after the reservation — the payment's, or the session's when it moved on before the transaction — gives the
/// reservation back, as a thrown failure does: the remote effect has its compensation, the transaction's rollback cannot
/// undo it.
/// </remarks>
public sealed class ConfirmCheckoutUseCase : IConfirmCheckoutInputPort
{
    /// <summary>How long giving back a reservation may take; not the caller's token, which may be what failed.</summary>
    private static readonly TimeSpan ReleaseDeadline = TimeSpan.FromSeconds(10);

    private readonly ICheckoutSessionRepository _sessions;
    private readonly ICheckoutArticleDataPort _articleData;
    private readonly IStockReservationPort _stockReservation;
    private readonly IPaymentProviderRegistry _providers;
    private readonly CheckoutPricing _pricing;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;
    private readonly ILogger<ConfirmCheckoutUseCase> _logger;

    public ConfirmCheckoutUseCase(
        ICheckoutSessionRepository sessions,
        ICheckoutArticleDataPort articleData,
        IStockReservationPort stockReservation,
        IPaymentProviderRegistry providers,
        CheckoutPricing pricing,
        IDomainEventPublisher events,
        ITransactionBoundary transactionBoundary,
        ILogger<ConfirmCheckoutUseCase> logger)
    {
        _transactionBoundary = transactionBoundary;
        _sessions = sessions;
        _articleData = articleData;
        _stockReservation = stockReservation;
        _providers = providers;
        _pricing = pricing;
        _events = events;
        _logger = logger;
    }

    public async Task<ConfirmCheckoutResult> ExecuteAsync(ConfirmCheckoutCommand command, CancellationToken cancellationToken = default)
    {
        var sessionId = new CheckoutSessionId(command.SessionId);

        // Article data comes from the Product Catalog (remote-capable) — fetched outside the transaction
        var customerId = CustomerId.Of(command.CustomerId);
        var current = await _sessions.FindByIdForCustomerAsync(sessionId, customerId, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return new ConfirmCheckoutResult.CheckoutSessionNotFound(sessionId);
        }

        var articles = await _articleData.GetArticleDataAsync(current.LineItems.Select(i => i.ProductId).ToArray(), cancellationToken).ConfigureAwait(false);
        var facts = articles.ToDictionary(e => e.Key, e => new ArticlePrice(e.Value.CurrentPrice, e.Value.IsAvailable, e.Value.AvailableStock));
        var shown = OrderFingerprint.From(command.Fingerprint);

        // Everything the session itself can refuse — an order changed since the review among it — is refused before the
        // stock is reserved: no order that cannot be placed holds stock back from another
        if (RefusalOf(() => current.AssertReadyForConfirmation(_pricing.ValidateItems(current, facts), facts, shown)) is { } notReady)
        {
            return notReady;
        }

        if (await ReserveStockAsync(current, cancellationToken).ConfigureAwait(false) is { } shortOfStock)
        {
            return shortOfStock;
        }

        // The payment comes last, outside the transaction (the provider is remote): for exactly the total the review
        // showed. A payment refused or not answered gives the reserved stock back, as any refusal after it does. Short
        // transaction then: reload, confirm, save, publish — the session can still have moved on since the check above,
        // superseded by a new checkout or confirmed in another tab.
        try
        {
            var (payment, refused) = await AuthorizePaymentAsync(current, current.TotalsAt(facts).Total, cancellationToken).ConfigureAwait(false);
            var result = refused ?? await _sessions.InCartSessionAsync(current.CartId, () => _transactionBoundary.InTransactionAsync<ConfirmCheckoutResult>(
                async ct =>
                {
                    var session = await _sessions.FindByIdForCustomerAsync(sessionId, customerId, ct).ConfigureAwait(false);
                    if (session is null)
                    {
                        return new ConfirmCheckoutResult.CheckoutSessionNotFound(sessionId);
                    }

                    if (RefusalOf(() => session.Confirm(_pricing.ValidateItems(session, facts), facts, shown, payment!)) is { } moved)
                    {
                        return moved;
                    }

                    await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                    await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                    return ConfirmCheckoutResult.Confirmed.From(session);
                },
                cancellationToken), cancellationToken).ConfigureAwait(false);

            if (result.Value is IRefusal)
            {
                await ReleaseQuietlyAsync(current).ConfigureAwait(false);
            }

            return result;
        }
        catch (Exception)
        {
            await ReleaseQuietlyAsync(current).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Has the provider the customer chose authorize <paramref name="amount"/> — the total the review showed — under the
    /// checkout's reference, and answers the chosen way to pay with that authorization. A provider the shop no longer
    /// knows, one that is not taking payments or does not answer, and one that refuses are each answered by their own case.
    /// </summary>
    private async Task<(PaymentSelection? Payment, ConfirmCheckoutResult? Refused)> AuthorizePaymentAsync(
        CheckoutSession session, Money amount, CancellationToken cancellationToken)
    {
        var chosen = session.PaymentSelection!;
        var provider = await _providers.FindAsync(chosen.ProviderId, cancellationToken).ConfigureAwait(false);
        if (provider is null)
        {
            return (null, new ConfirmCheckoutResult.PaymentProviderNotFound(chosen.ProviderId));
        }

        if (!await provider.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            return (null, new ConfirmCheckoutResult.PaymentProviderUnavailable(chosen.ProviderId));
        }

        try
        {
            var reference = await provider.InitiatePaymentAsync(session.Id, amount, cancellationToken).ConfigureAwait(false);
            return (chosen.Authorized(reference, amount), null);
        }
        catch (PaymentProviderUnavailableException unavailable)
        {
            _logger.LogWarning("Payment provider {ProviderId} is unavailable: {Reason}", chosen.ProviderId, unavailable.Message);
            return (null, new ConfirmCheckoutResult.PaymentProviderUnavailable(chosen.ProviderId));
        }
        catch (PaymentInitiationFailedException refused)
        {
            return (null, new ConfirmCheckoutResult.PaymentInitiationFailed(chosen.ProviderId, refused.Message));
        }
    }

    /// <summary>
    /// The session's refusal of <paramref name="decision"/> as a case — the model's refusals, caught once here; null
    /// when the session accepts. Any other failure keeps travelling.
    /// </summary>
    private static ConfirmCheckoutResult? RefusalOf(Action decision)
    {
        try
        {
            decision();
            return null;
        }
        catch (CheckoutNotModifiableException e)
        {
            return new ConfirmCheckoutResult.CheckoutNotModifiable(e.SessionId, e.Status);
        }
        catch (CheckoutStepNotCompletedException e)
        {
            return new ConfirmCheckoutResult.CheckoutStepNotCompleted(e.SessionId, e.Step);
        }
        catch (CheckoutStepOutOfOrderException e)
        {
            return new ConfirmCheckoutResult.CheckoutStepOutOfOrder(e.SessionId, e.Requested, e.Current);
        }
        catch (CheckoutValidationException e)
        {
            return new ConfirmCheckoutResult.CheckoutValidation(e.Validation);
        }
        catch (PriceChangedSinceReviewException e)
        {
            return new ConfirmCheckoutResult.PriceChangedSinceReview(e.SessionId, e.ProductName);
        }
        catch (OrderChangedSinceReviewException e)
        {
            return new ConfirmCheckoutResult.OrderChangedSinceReview(e.SessionId);
        }
    }

    /// <summary>
    /// Reserves the stock of the session's lines; when the stock no longer covers a line, the session refuses the
    /// confirmation naming it, as it refuses a line the facts already showed short.
    /// </summary>
    private async Task<ConfirmCheckoutResult?> ReserveStockAsync(CheckoutSession session, CancellationToken cancellationToken)
    {
        var shortages = await _stockReservation.ReserveAsync(session.LineItems, cancellationToken).ConfigureAwait(false);
        if (shortages.Count == 0)
        {
            return null;
        }

        var left = shortages.ToDictionary(shortage => shortage.ProductId, shortage => shortage.Left);
        var shortLines = session.LineItems
            .Where(line => left.ContainsKey(line.ProductId))
            .Select(line => ValidationError.InsufficientStock(line.ProductId, line.Quantity, left[line.ProductId], line.ProductName))
            .ToList();
        return RefusalOf(() => session.AssertReadyForConfirmation(CheckoutValidationResult.WithErrors(shortLines)));
    }

    /// <summary>
    /// Gives back the reservation of an order that was not placed. A release that fails is logged, not raised: the
    /// caller is told why the order was refused, not that the clean-up failed too; the stock stays held until an
    /// operator corrects it.
    /// </summary>
    private async Task ReleaseQuietlyAsync(CheckoutSession session)
    {
        using var deadline = new CancellationTokenSource(ReleaseDeadline);
        try
        {
            await _stockReservation.ReleaseAsync(session.LineItems, deadline.Token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "The stock reserved for checkout {SessionId} could not be released", session.Id);
        }
    }
}