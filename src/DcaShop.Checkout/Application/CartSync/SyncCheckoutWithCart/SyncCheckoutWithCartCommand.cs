namespace DcaShop.Checkout.Application.CartSync.SyncCheckoutWithCart;

/// <summary>Synchronise the active checkout session of the cart that changed.</summary>
public sealed record SyncCheckoutWithCartCommand(Guid CartId);
