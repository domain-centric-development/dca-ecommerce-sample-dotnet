using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>The delivery page: the session, the shipping options to choose from and the error it shows.</summary>
public sealed record DeliveryPageViewModel(
    CheckoutCartSnapshot Session,
    IReadOnlyList<DeliveryPageViewModel.ShippingChoice> ShippingOptions,
    string? Error)
{
    public sealed record ShippingChoice(string Id, string Name, string EstimatedDelivery, string Cost, string CurrencyCode);
}