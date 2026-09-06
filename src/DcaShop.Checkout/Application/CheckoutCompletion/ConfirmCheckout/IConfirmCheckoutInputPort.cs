using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;

public interface IConfirmCheckoutInputPort : IUseCase<ConfirmCheckoutCommand, ConfirmCheckoutResult>
{
}
