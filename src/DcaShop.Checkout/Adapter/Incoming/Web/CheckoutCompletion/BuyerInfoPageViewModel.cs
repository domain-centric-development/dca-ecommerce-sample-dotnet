using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>The buyer information page: the session, the error it shows and who is buying — a guest or the logged-in customer.</summary>
public sealed record BuyerInfoPageViewModel(CheckoutCartSnapshot Session, string? Error, bool IsAnonymous, string? IdentityEmail);