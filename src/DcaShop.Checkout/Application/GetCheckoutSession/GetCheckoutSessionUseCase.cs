using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.GetCheckoutSession;

public sealed class GetCheckoutSessionUseCase : IGetCheckoutSessionInputPort
{
    private readonly ICheckoutSessionRepository _sessions;

    public GetCheckoutSessionUseCase(ICheckoutSessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<GetCheckoutSessionResult> ExecuteAsync(GetCheckoutSessionQuery query, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.FindByIdAsync(new CheckoutSessionId(query.SessionId), cancellationToken).ConfigureAwait(false);
        return new GetCheckoutSessionResult(session is null ? null : CheckoutCartSnapshot.From(session));
    }
}
