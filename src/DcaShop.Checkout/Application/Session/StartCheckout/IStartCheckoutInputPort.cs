using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.StartCheckout;

public interface IStartCheckoutInputPort : IUseCase<StartCheckoutCommand, StartCheckoutResult>
{
}
