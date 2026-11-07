using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when the payment provider answered and refused to open a payment for this checkout.</summary>
/// <remarks>
/// Part of the <see cref="IPaymentProvider"/> contract. A failure the output port declares, so an
/// <see cref="OutputPortException"/>: the adapter throws it, the use case catches it and answers with a case of its
/// result. The provider's own reason is carried through unchanged for logs; the shop has no way to judge it, and the
/// page shows the customer a fixed message of its own instead.
/// </remarks>
public sealed class PaymentInitiationFailedException : OutputPortException
{
    public PaymentInitiationFailedException(PaymentProviderId providerId, string reason)
        : base(reason)
    {
        ProviderId = providerId;
    }

    public PaymentProviderId ProviderId { get; }
}