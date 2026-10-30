using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when the addressed checkout session has no buyer info: its first step was never submitted.</summary>
/// <remarks>
/// A confirmed checkout always has one, so an order confirmation reaches this only for a session that was never
/// confirmed. Named so the log says which rule the delivery broke; no customer sees it.
/// </remarks>
public sealed class BuyerInfoNotSubmittedException : UseCaseException
{
    public BuyerInfoNotSubmittedException(CheckoutSessionId sessionId)
        : base($"Checkout {sessionId.Value} has no buyer info")
    {
        SessionId = sessionId;
    }

    public CheckoutSessionId SessionId { get; }
}