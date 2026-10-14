using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>The payment page: the session, the ways to pay and the error it shows.</summary>
public sealed record PaymentPageViewModel(
    CheckoutCartSnapshot Session,
    IReadOnlyList<PaymentPageViewModel.PaymentChoice> PaymentProviders,
    string? Error)
{
    public sealed record PaymentChoice(string Id, string DisplayName, bool Available);
}