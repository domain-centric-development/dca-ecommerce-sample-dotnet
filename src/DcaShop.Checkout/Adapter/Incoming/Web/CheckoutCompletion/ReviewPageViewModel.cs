using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>
/// The review page: the order to place at today's prices, the fingerprint its "Place Order" carries and the error it
/// shows — a refused "Place Order" among them.
/// </summary>
public sealed record ReviewPageViewModel(CheckoutCartSnapshot Session, string Fingerprint, string? Error);