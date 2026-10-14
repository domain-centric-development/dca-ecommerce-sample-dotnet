using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>The confirmation page: the confirmed session.</summary>
public sealed record ConfirmationPageViewModel(CheckoutCartSnapshot Session);