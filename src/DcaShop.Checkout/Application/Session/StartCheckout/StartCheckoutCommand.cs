namespace DcaShop.Checkout.Application.StartCheckout;

/// <summary>
/// Starts a checkout for one of <paramref name="CustomerId"/>'s carts. The cart id arrives from the browser, so
/// it names <i>which</i> cart — never <i>whose</i>; without the customer the command would let anyone start a
/// checkout on a cart whose id they happened to learn.
/// </summary>
public sealed record StartCheckoutCommand(Guid CartId, string CustomerId);
