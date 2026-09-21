using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when the addressed checkout session is not available to the asking customer.</summary>
/// <remarks>
/// One failure for two situations, on purpose: no such session, and somebody else's session. The lookup asks
/// for the session <em>of this customer</em>, so a stranger cannot learn which session identities exist
/// (ADR-007).
/// </remarks>
public sealed class CheckoutSessionNotFoundException : UseCaseException
{
    public CheckoutSessionNotFoundException(CheckoutSessionId sessionId)
        : base($"Session not found: {sessionId.Value}")
    {
        SessionId = sessionId;
    }

    public CheckoutSessionId SessionId { get; }
}
