namespace DcaShop.Cart.Application.GetCartMergeOptions;

/// <summary>Asks whether the visitor has to decide between two carts after logging in.</summary>
public sealed record GetCartMergeOptionsQuery(string AnonymousUserId, string RegisteredUserId);
