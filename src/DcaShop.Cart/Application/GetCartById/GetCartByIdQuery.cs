namespace DcaShop.Cart.Application.GetCartById;

/// <summary>
/// Reads one of <paramref name="CustomerId"/>'s carts. The cart id says <i>which</i> cart is meant; the customer
/// id says <i>whose</i> — without it the query would be unanswerable without guessing, and every adapter would
/// have to guard it for itself.
/// </summary>
public sealed record GetCartByIdQuery(Guid CartId, string CustomerId);
