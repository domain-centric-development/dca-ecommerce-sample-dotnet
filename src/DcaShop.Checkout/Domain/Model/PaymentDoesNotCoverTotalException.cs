using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>
/// Raised when an order would be confirmed with a payment that does not pay exactly its total through the provider the
/// customer chose — no payment reference, another provider, or another amount.
/// </summary>
/// <remarks>
/// The provider holds what it authorized, not what the order costs: an order confirmed for another amount would be an
/// order nobody pays, or one the customer pays more for than they ordered.
/// </remarks>
public sealed class PaymentDoesNotCoverTotalException : DomainException
{
    public PaymentDoesNotCoverTotalException(CheckoutSessionId sessionId, Money? authorizedAmount, Money total)
        : base($"The payment does not pay exactly the total of {total} through the chosen provider (authorized: {authorizedAmount?.ToString() ?? "nothing"})")
    {
        SessionId = sessionId;
        AuthorizedAmount = authorizedAmount;
        Total = total;
    }

    public CheckoutSessionId SessionId { get; }

    public Money? AuthorizedAmount { get; }

    public Money Total { get; }
}