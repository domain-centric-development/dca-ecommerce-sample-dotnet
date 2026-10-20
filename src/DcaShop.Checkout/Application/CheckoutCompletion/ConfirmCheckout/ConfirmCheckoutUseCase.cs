using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Microsoft.Extensions.Logging;

namespace DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;

/// <summary>
/// Places the order: checks every line against the facts of this moment, reserves the order's stock in Inventory and
/// only then confirms the session. Of two orders placed at once, the one whose reservation the stock no longer covers
/// is refused, naming the line, and changes nothing.
/// </summary>
public sealed class ConfirmCheckoutUseCase : IConfirmCheckoutInputPort
{
    /// <summary>How long giving back a reservation may take; not the caller's token, which may be what failed.</summary>
    private static readonly TimeSpan ReleaseDeadline = TimeSpan.FromSeconds(10);

    private readonly ICheckoutSessionRepository _sessions;
    private readonly ICheckoutArticleDataPort _articleData;
    private readonly IStockReservationPort _stockReservation;
    private readonly CheckoutPricing _pricing;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;
    private readonly ILogger<ConfirmCheckoutUseCase> _logger;

    public ConfirmCheckoutUseCase(
        ICheckoutSessionRepository sessions,
        ICheckoutArticleDataPort articleData,
        IStockReservationPort stockReservation,
        CheckoutPricing pricing,
        IDomainEventPublisher events,
        ITransactionBoundary transactionBoundary,
        ILogger<ConfirmCheckoutUseCase> logger)
    {
        _transactionBoundary = transactionBoundary;
        _sessions = sessions;
        _articleData = articleData;
        _stockReservation = stockReservation;
        _pricing = pricing;
        _events = events;
        _logger = logger;
    }

    public async Task<ConfirmCheckoutResult> ExecuteAsync(ConfirmCheckoutCommand command, CancellationToken cancellationToken = default)
    {
        var sessionId = new CheckoutSessionId(command.SessionId);

        // Article data comes from the Product Catalog (remote-capable) — fetched outside the transaction
        var customerId = CustomerId.Of(command.CustomerId);
        var current = await LoadAsync(sessionId, customerId, cancellationToken).ConfigureAwait(false);
        var articles = await _articleData.GetArticleDataAsync(current.LineItems.Select(i => i.ProductId).ToArray(), cancellationToken).ConfigureAwait(false);
        var facts = articles.ToDictionary(e => e.Key, e => new ArticlePrice(e.Value.CurrentPrice, e.Value.IsAvailable, e.Value.AvailableStock));

        // Everything the session itself can refuse is refused before the stock is reserved: no order that cannot be
        // placed holds stock back from another
        current.AssertReadyForConfirmation(_pricing.ValidateItems(current, facts));
        await ReserveStockAsync(current, cancellationToken).ConfigureAwait(false);

        // Short transaction: reload, confirm, save, publish. The session can still have moved on since the check above
        // — superseded by a new checkout, confirmed in another tab — so the stock already reserved is given back.
        try
        {
            return await _sessions.InCartSessionAsync(current.CartId, () => _transactionBoundary.InTransactionAsync(
                async ct =>
                {
                    var session = await LoadAsync(sessionId, customerId, ct).ConfigureAwait(false);
                    session.Confirm(_pricing.ValidateItems(session, facts), _pricing.CalculateOrderTotal(session, facts));
                    await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                    await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                    return ConfirmCheckoutResult.From(session);
                },
                cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await ReleaseQuietlyAsync(current).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Reserves the stock of the session's lines; when the stock no longer covers a line, the session refuses the
    /// confirmation naming it, as it refuses a line the facts already showed short.
    /// </summary>
    private async Task ReserveStockAsync(CheckoutSession session, CancellationToken cancellationToken)
    {
        var shortages = await _stockReservation.ReserveAsync(session.LineItems, cancellationToken).ConfigureAwait(false);
        if (shortages.Count == 0)
        {
            return;
        }

        var left = shortages.ToDictionary(shortage => shortage.ProductId, shortage => shortage.Left);
        var shortLines = session.LineItems
            .Where(line => left.ContainsKey(line.ProductId))
            .Select(line => ValidationError.InsufficientStock(line.ProductId, line.Quantity, left[line.ProductId], line.ProductName))
            .ToList();
        session.AssertReadyForConfirmation(CheckoutValidationResult.WithErrors(shortLines));
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

    /// <summary>Loads the session as the caller's own: one that is not theirs is not found.</summary>
    private async Task<CheckoutSession> LoadAsync(CheckoutSessionId sessionId, CustomerId customerId, CancellationToken cancellationToken) =>
        await _sessions.FindByIdForCustomerAsync(sessionId, customerId, cancellationToken).ConfigureAwait(false)
        ?? throw new CheckoutSessionNotFoundException(sessionId);
}