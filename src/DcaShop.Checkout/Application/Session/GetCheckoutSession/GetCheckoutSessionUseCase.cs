using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Application.Session.GetCheckoutSession;

/// <summary>
/// Reads one checkout session of the customer. When the query names a step, the domain decides whether it may be
/// opened and the decision travels in the result; the adapter only turns it into a route.
/// </summary>
public sealed class GetCheckoutSessionUseCase : IGetCheckoutSessionInputPort
{
    private readonly ICheckoutSessionRepository _sessions;

    public GetCheckoutSessionUseCase(ICheckoutSessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<GetCheckoutSessionResult> ExecuteAsync(GetCheckoutSessionQuery query, CancellationToken cancellationToken = default)
    {
        var sessionId = new CheckoutSessionId(query.SessionId);
        var session = await _sessions.FindByIdForCustomerAsync(sessionId, CustomerId.Of(query.CustomerId), cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return new GetCheckoutSessionResult.CheckoutSessionNotFound(sessionId);
        }

        var snapshot = CheckoutCartSnapshot.From(session);
        return new GetCheckoutSessionResult.Found(snapshot, query.RequestedStep is { } step ? snapshot.AccessTo(step) : null);
    }
}