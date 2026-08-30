using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.GetConfirmedCheckoutSession;

public interface IGetConfirmedCheckoutSessionInputPort : IUseCase<GetConfirmedCheckoutSessionQuery, GetConfirmedCheckoutSessionResult>
{
}
