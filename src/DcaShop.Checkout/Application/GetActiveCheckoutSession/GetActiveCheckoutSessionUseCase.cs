using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.GetActiveCheckoutSession;

/// <summary>Reads the customer's active checkout session, if any — the web adapter resolves the current session from it instead of carrying session ids in URLs.</summary>
public sealed class GetActiveCheckoutSessionUseCase : IGetActiveCheckoutSessionInputPort
{
    private readonly ICheckoutSessionRepository _sessions;

    public GetActiveCheckoutSessionUseCase(ICheckoutSessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<GetActiveCheckoutSessionResult> ExecuteAsync(GetActiveCheckoutSessionQuery query, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.FindActiveByCustomerAsync(CustomerId.Of(query.CustomerId), cancellationToken).ConfigureAwait(false);
        return new GetActiveCheckoutSessionResult(session is null ? null : CheckoutCartSnapshot.From(session));
    }
}
