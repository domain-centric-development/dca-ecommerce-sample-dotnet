namespace DcaShop.Cart.Application.RecoverCartOnLogin;

/// <summary>
/// Brings the cart a visitor filled as a guest over to the account they just logged into.
/// </summary>
public sealed record RecoverCartOnLoginCommand(string AnonymousUserId, string RegisteredUserId);
