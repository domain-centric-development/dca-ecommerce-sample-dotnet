using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Selected payment method plus an optional provider reference (e.g. a payment intent id).</summary>
public sealed record PaymentSelection(PaymentProviderId ProviderId, string? ProviderReference = null) : IValue
{
    public bool HasReference => !string.IsNullOrWhiteSpace(ProviderReference);

    public PaymentSelection WithReference(string reference) => this with { ProviderReference = reference };
}
