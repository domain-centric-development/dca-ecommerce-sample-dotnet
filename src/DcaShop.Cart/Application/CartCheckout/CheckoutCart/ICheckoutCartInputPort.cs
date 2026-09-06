using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.CartCheckout.CheckoutCart;

public interface ICheckoutCartInputPort : IUseCase<CheckoutCartCommand, CheckoutCartResult>
{
}
