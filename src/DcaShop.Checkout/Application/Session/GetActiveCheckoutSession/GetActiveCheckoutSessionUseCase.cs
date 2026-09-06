using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;

namespace DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;

/// <summary>
/// Reads the customer's active checkout session, if any — the web adapter resolves the current session from it instead
/// of carrying session ids in URLs. When the query names a step, the domain decides whether it may be opened and the
/// decision travels in the result; the adapter only turns it into a route.
/// </summary>
public sealed class GetActiveCheckoutSessionUseCase : IGetActiveCheckoutSessionInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly CheckoutStepValidator _stepValidator;

    public GetActiveCheckoutSessionUseCase(ICheckoutSessionRepository sessions, CheckoutStepValidator stepValidator)
    {
        _sessions = sessions;
        _stepValidator = stepValidator;
    }

    public async Task<GetActiveCheckoutSessionResult> ExecuteAsync(GetActiveCheckoutSessionQuery query, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.FindActiveByCustomerAsync(CustomerId.Of(query.CustomerId), cancellationToken).ConfigureAwait(false);
        var snapshot = session is null ? null : CheckoutCartSnapshot.From(session);
        var access = query.RequestedStep is { } step ? _stepValidator.AccessTo(snapshot, step) : null;
        return new GetActiveCheckoutSessionResult(snapshot, access);
    }
}
