using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>The review page: the session to place and the error it shows — a refused "Place Order" among them.</summary>
public sealed record ReviewPageViewModel(CheckoutCartSnapshot Session, string? Error);