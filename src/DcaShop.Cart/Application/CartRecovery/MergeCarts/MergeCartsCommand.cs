namespace DcaShop.Cart.Application.CartRecovery.MergeCarts;

/// <summary>
/// Reconciles the cart of the identity a visitor had before login with the account's own. <see cref="GuestBeforeLogin"/>
/// is the guest the caller was before that login, absent where none is known; only that guest's cart is merged.
/// </summary>
public sealed record MergeCartsCommand(
    string AnonymousUserId, string RegisteredUserId, CartMergeStrategy Strategy, string? GuestBeforeLogin)
{
    public string AnonymousUserId { get; } = string.IsNullOrWhiteSpace(AnonymousUserId)
        ? throw new ArgumentException("Anonymous user ID cannot be null or blank", nameof(AnonymousUserId))
        : AnonymousUserId;

    public string RegisteredUserId { get; } = string.IsNullOrWhiteSpace(RegisteredUserId)
        ? throw new ArgumentException("Registered user ID cannot be null or blank", nameof(RegisteredUserId))
        : RegisteredUserId;
}