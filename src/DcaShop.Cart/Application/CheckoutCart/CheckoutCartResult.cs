namespace DcaShop.Cart.Application.CheckoutCart;

public sealed record CheckoutCartResult(Guid CartId, string Status, string Total);
