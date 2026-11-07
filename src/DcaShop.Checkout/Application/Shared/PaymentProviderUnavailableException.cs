using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>
/// Raised when the payment provider did not answer in time, could not be reached, or answered outside its contract.
/// </summary>
/// <remarks>
/// Part of the <see cref="IPaymentProvider"/> contract: the provider is a foreign system, and its being unreachable is
/// expected behaviour of the integration. A failure the output port declares, so an <see cref="OutputPortException"/>:
/// the adapter throws it, the use case catches it and answers with a case of its result. Different from an unknown
/// provider: the choice was valid, the outside world is not ready, and trying again later is the sensible answer.
/// </remarks>
public sealed class PaymentProviderUnavailableException : OutputPortException
{
    public PaymentProviderUnavailableException(PaymentProviderId providerId, string reason)
        : base(reason)
    {
        ProviderId = providerId;
    }

    public PaymentProviderId ProviderId { get; }
}