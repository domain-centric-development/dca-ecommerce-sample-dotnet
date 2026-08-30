using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>View model shared by all checkout pages; the markup mirrors the Java sample's checkout views.</summary>
public sealed record CheckoutPageViewModel(
    CheckoutSessionData Session,
    IReadOnlyList<CheckoutPageViewModel.ShippingChoice> ShippingOptions,
    IReadOnlyList<CheckoutPageViewModel.PaymentChoice> PaymentProviders,
    string? Error)
{
    public sealed record ShippingChoice(string Id, string Name, string EstimatedDelivery, string Cost, string CurrencyCode);

    public sealed record PaymentChoice(string Id, string DisplayName, string Description, bool Available);

    public bool IsStep(string step) => Session.CurrentStep == step;
}
