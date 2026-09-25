using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when the payment provider refused to open a payment for this checkout.</summary>
/// <remarks>
/// The provider's own reason is carried through unchanged; the shop has no way to judge it and the customer
/// needs to see what the provider said.
/// </remarks>
public sealed class PaymentInitiationFailedException : UseCaseException
{
    public PaymentInitiationFailedException(PaymentProviderId providerId, string reason)
        : base(reason)
    {
        ProviderId = providerId;
    }

    public PaymentProviderId ProviderId { get; }
}