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
        var session = await _sessions.FindByIdForCustomerAsync(new CheckoutSessionId(query.SessionId), CustomerId.Of(query.CustomerId), cancellationToken).ConfigureAwait(false);
        var snapshot = session is null ? null : CheckoutCartSnapshot.From(session);
        var access = query.RequestedStep is { } step
            ? snapshot?.AccessTo(step) ?? StepAccess.BackToCart()
            : null;
        return new GetCheckoutSessionResult(snapshot, access);
    }
}