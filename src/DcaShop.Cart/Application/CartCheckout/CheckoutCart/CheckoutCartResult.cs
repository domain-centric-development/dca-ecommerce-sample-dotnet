using DcaShop.SharedKernel.Domain.Model;
namespace DcaShop.Cart.Application.CartCheckout.CheckoutCart;

public sealed record CheckoutCartResult(Guid CartId, string Status, Money Total);
