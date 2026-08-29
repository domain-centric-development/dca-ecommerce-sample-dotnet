using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.GetCheckoutSession;

public interface IGetCheckoutSessionInputPort : IUseCase<GetCheckoutSessionQuery, GetCheckoutSessionResult>
{
}
