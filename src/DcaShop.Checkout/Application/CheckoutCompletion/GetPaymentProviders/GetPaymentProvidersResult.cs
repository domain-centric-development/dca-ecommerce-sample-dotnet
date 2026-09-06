namespace DcaShop.Checkout.Application.CheckoutCompletion.GetPaymentProviders;

public sealed record GetPaymentProvidersResult(IReadOnlyList<GetPaymentProvidersResult.PaymentProviderData> Providers)
{
    public sealed record PaymentProviderData(string Id, string DisplayName);
}
