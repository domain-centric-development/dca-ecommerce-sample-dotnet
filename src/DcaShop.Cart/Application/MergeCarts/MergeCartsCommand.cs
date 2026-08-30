namespace DcaShop.Cart.Application.MergeCarts;

/// <summary>Reconciles the cart of the identity a visitor had before login with the account's own.</summary>
public sealed record MergeCartsCommand(string AnonymousUserId, string RegisteredUserId, CartMergeStrategy Strategy)
{
    public string AnonymousUserId { get; } = string.IsNullOrWhiteSpace(AnonymousUserId)
        ? throw new ArgumentException("Anonymous user ID cannot be null or blank", nameof(AnonymousUserId))
        : AnonymousUserId;

    public string RegisteredUserId { get; } = string.IsNullOrWhiteSpace(RegisteredUserId)
        ? throw new ArgumentException("Registered user ID cannot be null or blank", nameof(RegisteredUserId))
        : RegisteredUserId;
}
