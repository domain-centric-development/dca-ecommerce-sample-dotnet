using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.GetActiveCheckoutSession;

public interface IGetActiveCheckoutSessionInputPort : IUseCase<GetActiveCheckoutSessionQuery, GetActiveCheckoutSessionResult>
{
}
