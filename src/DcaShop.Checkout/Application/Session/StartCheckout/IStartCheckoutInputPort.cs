using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.Session.StartCheckout;

public interface IStartCheckoutInputPort : IUseCase<StartCheckoutCommand, StartCheckoutResult>
{
}
