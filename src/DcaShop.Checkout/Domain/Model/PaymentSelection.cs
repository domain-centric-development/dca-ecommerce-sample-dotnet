using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>
/// Selected payment method plus an optional provider reference (e.g. a payment intent id) and the amount the provider
/// authorized under it.
/// </summary>
public sealed record PaymentSelection(PaymentProviderId ProviderId, string? ProviderReference = null, Money? AuthorizedAmount = null) : IValue
{
    public bool HasReference => !string.IsNullOrWhiteSpace(ProviderReference);

    public PaymentSelection WithReference(string reference) => this with { ProviderReference = reference };

    /// <summary>The chosen way to pay, once its provider authorized <paramref name="amount"/> under <paramref name="reference"/>.</summary>
    public PaymentSelection Authorized(string reference, Money amount) =>
        this with { ProviderReference = reference, AuthorizedAmount = amount };

    /// <summary>Whether this payment, authorized by <paramref name="providerId"/>, pays exactly <paramref name="total"/>.</summary>
    public bool Covers(PaymentProviderId providerId, Money total) =>
        HasReference && ProviderId == providerId && AuthorizedAmount == total;
}