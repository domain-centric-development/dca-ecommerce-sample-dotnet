using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;

public interface IGetActiveCheckoutSessionInputPort : IUseCase<GetActiveCheckoutSessionQuery, GetActiveCheckoutSessionResult>
{
}
