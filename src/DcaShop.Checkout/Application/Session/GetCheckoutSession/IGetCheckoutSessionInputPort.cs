using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.Session.GetCheckoutSession;

public interface IGetCheckoutSessionInputPort : IUseCase<GetCheckoutSessionQuery, GetCheckoutSessionResult>
{
}
