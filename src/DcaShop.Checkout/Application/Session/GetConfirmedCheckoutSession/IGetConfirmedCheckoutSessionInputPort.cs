using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.Session.GetConfirmedCheckoutSession;

public interface IGetConfirmedCheckoutSessionInputPort : IUseCase<GetConfirmedCheckoutSessionQuery, GetConfirmedCheckoutSessionResult>
{
}
