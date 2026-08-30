using DomainCentric.BuildingBlocks.Application.Transactions;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.ConfirmCheckout;

public sealed class ConfirmCheckoutUseCase : IConfirmCheckoutInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly ICheckoutArticleDataPort _articleData;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public ConfirmCheckoutUseCase(ICheckoutSessionRepository sessions, ICheckoutArticleDataPort articleData, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _sessions = sessions;
        _articleData = articleData;
        _events = events;
    }

    public async Task<ConfirmCheckoutResult> ExecuteAsync(ConfirmCheckoutCommand command, CancellationToken cancellationToken = default)
    {
        var sessionId = new CheckoutSessionId(command.SessionId);

        // Article data comes from the Product Catalog (remote-capable) — fetched outside the transaction
        var current = await LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var articles = await _articleData.GetArticleDataAsync(current.LineItems.Select(i => i.ProductId).ToArray(), cancellationToken).ConfigureAwait(false);
        var resolver = new ArticleDataPriceResolver(articles);

        // Short transaction: reload, confirm, save, publish
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var session = await LoadAsync(sessionId, ct).ConfigureAwait(false);
                session.Confirm(resolver);
                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                return new ConfirmCheckoutResult(CheckoutSessionData.From(session));
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<CheckoutSession> LoadAsync(CheckoutSessionId sessionId, CancellationToken cancellationToken) =>
        await _sessions.FindByIdAsync(sessionId, cancellationToken).ConfigureAwait(false)
        ?? throw new ArgumentException($"Session not found: {sessionId}", nameof(sessionId));
}
