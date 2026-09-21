using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when the selected payment provider is known but cannot take a payment right now.</summary>
/// <remarks>
/// Different from an unknown provider: the choice was valid, the outside world is not ready, and trying again
/// later is the sensible answer.
/// </remarks>
public sealed class PaymentProviderUnavailableException : UseCaseException
{
    public PaymentProviderUnavailableException(PaymentProviderId providerId)
        : base($"Payment provider is currently unavailable: {providerId}")
    {
        ProviderId = providerId;
    }

    public PaymentProviderId ProviderId { get; }
}
