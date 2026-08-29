using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>Everything the checkout pages show: the session and, per step, the choices offered.</summary>
public sealed record CheckoutPageViewModel(
    CheckoutSessionData Session,
    IReadOnlyList<CheckoutPageViewModel.ShippingChoice> ShippingOptions,
    IReadOnlyList<CheckoutPageViewModel.PaymentChoice> PaymentProviders,
    string? Error)
{
    public sealed record ShippingChoice(string Id, string Name, string EstimatedDelivery, string Cost);

    public sealed record PaymentChoice(string Id, string DisplayName, string Description);

    public bool IsStep(string step) => Session.CurrentStep == step;
}
