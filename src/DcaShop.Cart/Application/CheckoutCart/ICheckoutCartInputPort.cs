using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.CheckoutCart;

public interface ICheckoutCartInputPort : IUseCase<CheckoutCartCommand, CheckoutCartResult>
{
}
