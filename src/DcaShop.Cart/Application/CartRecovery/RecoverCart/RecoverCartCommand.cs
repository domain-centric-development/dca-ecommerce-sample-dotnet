namespace DcaShop.Cart.Application.CartRecovery.RecoverCart;

/// <summary>
/// Brings the cart a visitor filled as a guest over to the account they just logged into. <see cref="GuestBeforeLogin"/>
/// is the guest the caller was before that login, absent where none is known; only that guest's cart is taken over.
/// </summary>
public sealed record RecoverCartCommand(string AnonymousUserId, string RegisteredUserId, string? GuestBeforeLogin);