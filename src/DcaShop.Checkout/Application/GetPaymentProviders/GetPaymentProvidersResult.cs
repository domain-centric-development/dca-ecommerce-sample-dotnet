using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.GetPaymentProviders;

public sealed record GetPaymentProvidersResult(IReadOnlyList<GetPaymentProvidersResult.PaymentProviderData> Providers)
{
    public sealed record PaymentProviderData(string Id, string DisplayName, string Description);
}
