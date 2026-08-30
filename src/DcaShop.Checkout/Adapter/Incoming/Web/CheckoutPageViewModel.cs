using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>View model shared by all checkout pages; the markup mirrors the Java sample's checkout views.</summary>
public sealed record CheckoutPageViewModel(
    CheckoutCartSnapshot Session,
    IReadOnlyList<CheckoutPageViewModel.ShippingChoice> ShippingOptions,
    IReadOnlyList<CheckoutPageViewModel.PaymentChoice> PaymentProviders,
    string? Error)
{
    public sealed record ShippingChoice(string Id, string Name, string EstimatedDelivery, string Cost, string CurrencyCode);

    public sealed record PaymentChoice(string Id, string DisplayName, string Description, bool Available);

    public bool IsStep(CheckoutStep step) => Session.Step == step;
}
