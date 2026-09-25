using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Raised when a payment would be arranged for a checkout that owes nothing.</summary>
/// <remarks>
/// A payment intent for a total of zero would ask a provider to charge nothing, which every provider answers
/// differently and none of them usefully.
/// </remarks>
public sealed class NothingToPayException : DomainException
{
    public NothingToPayException(CheckoutSessionId sessionId, Money total)
        : base($"Nothing to pay: the total is {total}")
    {
        SessionId = sessionId;
    }

    public CheckoutSessionId SessionId { get; }
}