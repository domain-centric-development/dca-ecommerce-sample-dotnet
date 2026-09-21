using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when the payment provider a customer selected is not one the shop offers.</summary>
/// <remarks>
/// The registry is the shop's list of providers; an identifier outside it names nothing this checkout can
/// charge with.
/// </remarks>
public sealed class PaymentProviderNotFoundException : UseCaseException
{
    public PaymentProviderNotFoundException(PaymentProviderId providerId)
        : base($"Payment provider not found: {providerId}")
    {
        ProviderId = providerId;
    }

    public PaymentProviderId ProviderId { get; }
}
