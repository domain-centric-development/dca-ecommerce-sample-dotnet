using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Application.CheckoutCompletion.GetReview;

/// <summary>
/// Reads the customer's checkout as the review shows it: at today's prices, with the fingerprint "Place Order" carries
/// back. Changes nothing — the prices are the article facts of this moment, not stored on the session.
/// </summary>
public sealed class GetReviewUseCase : IGetReviewInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly ICheckoutArticleDataPort _articleData;

    public GetReviewUseCase(ICheckoutSessionRepository sessions, ICheckoutArticleDataPort articleData)
    {
        _sessions = sessions;
        _articleData = articleData;
    }

    public async Task<GetReviewResult> ExecuteAsync(GetReviewQuery query, CancellationToken cancellationToken = default)
    {
        var sessionId = new CheckoutSessionId(query.SessionId);
        var session = await _sessions.FindByIdForCustomerAsync(sessionId, CustomerId.Of(query.CustomerId), cancellationToken).ConfigureAwait(false)
            ?? throw new CheckoutSessionNotFoundException(sessionId);
        var articles = await _articleData.GetArticleDataAsync(session.LineItems.Select(i => i.ProductId).ToArray(), cancellationToken).ConfigureAwait(false);
        var facts = articles.ToDictionary(e => e.Key, e => new ArticlePrice(e.Value.CurrentPrice, e.Value.IsAvailable, e.Value.AvailableStock));
        return new GetReviewResult(CheckoutCartSnapshot.AtTodaysPrices(session, facts), session.FingerprintAt(facts).Value);
    }
}