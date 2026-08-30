namespace DcaShop.Cart.Application.CheckoutCart;

/// <summary>Checks out one of <paramref name="CustomerId"/>'s carts.</summary>
public sealed record CheckoutCartCommand(Guid CartId, string CustomerId);
