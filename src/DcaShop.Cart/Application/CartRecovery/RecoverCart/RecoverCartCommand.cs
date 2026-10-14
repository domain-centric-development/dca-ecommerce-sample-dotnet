namespace DcaShop.Cart.Application.CartRecovery.RecoverCart;

/// <summary>
/// Brings the cart a visitor filled as a guest over to the account they just logged into.
/// </summary>
public sealed record RecoverCartCommand(string AnonymousUserId, string RegisteredUserId);