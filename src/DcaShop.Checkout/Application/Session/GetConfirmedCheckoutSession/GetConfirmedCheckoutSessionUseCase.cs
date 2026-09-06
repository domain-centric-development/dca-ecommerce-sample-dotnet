using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.GetConfirmedCheckoutSession;

/// <summary>Reads the customer's most recent confirmed or completed session — what the confirmation page shows.</summary>
public sealed class GetConfirmedCheckoutSessionUseCase : IGetConfirmedCheckoutSessionInputPort
{
    private readonly ICheckoutSessionRepository _sessions;

    public GetConfirmedCheckoutSessionUseCase(ICheckoutSessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<GetConfirmedCheckoutSessionResult> ExecuteAsync(GetConfirmedCheckoutSessionQuery query, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.FindConfirmedOrCompletedByCustomerAsync(CustomerId.Of(query.CustomerId), cancellationToken).ConfigureAwait(false);
        return new GetConfirmedCheckoutSessionResult(session is null ? null : CheckoutCartSnapshot.From(session));
    }
}
