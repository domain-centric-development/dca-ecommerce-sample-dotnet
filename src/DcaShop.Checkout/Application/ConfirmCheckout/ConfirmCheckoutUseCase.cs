using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.ConfirmCheckout;

public sealed class ConfirmCheckoutUseCase : IConfirmCheckoutInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly ICheckoutArticleDataPort _articleData;
    private readonly IDomainEventPublisher _events;

    public ConfirmCheckoutUseCase(ICheckoutSessionRepository sessions, ICheckoutArticleDataPort articleData, IDomainEventPublisher events)
    {
        _sessions = sessions;
        _articleData = articleData;
        _events = events;
    }

    public async Task<ConfirmCheckoutResult> ExecuteAsync(ConfirmCheckoutCommand command, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.FindByIdAsync(new CheckoutSessionId(command.SessionId), cancellationToken).ConfigureAwait(false)
                      ?? throw new ArgumentException($"Session not found: {command.SessionId}", nameof(command));

        var articles = await _articleData.GetArticleDataAsync(session.LineItems.Select(i => i.ProductId).ToArray(), cancellationToken).ConfigureAwait(false);
        session.Confirm(new ArticleDataPriceResolver(articles));

        await _sessions.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(session, cancellationToken).ConfigureAwait(false);

        return new ConfirmCheckoutResult(CheckoutSessionData.From(session));
    }
}
