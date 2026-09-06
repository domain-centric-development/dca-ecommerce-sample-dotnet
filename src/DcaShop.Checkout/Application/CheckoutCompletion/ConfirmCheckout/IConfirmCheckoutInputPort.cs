using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.ConfirmCheckout;

public interface IConfirmCheckoutInputPort : IUseCase<ConfirmCheckoutCommand, ConfirmCheckoutResult>
{
}
