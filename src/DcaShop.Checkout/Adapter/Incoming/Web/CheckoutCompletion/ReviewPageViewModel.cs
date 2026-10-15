using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>
/// The review page: the session to place, the error it shows — a refused "Place Order" among them — and the information
/// it shows without refusing anything.
/// </summary>
public sealed record ReviewPageViewModel(CheckoutCartSnapshot Session, string? Error, string? Information = null);