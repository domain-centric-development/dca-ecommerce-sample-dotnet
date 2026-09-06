using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.SubmitDelivery;

public interface ISubmitDeliveryInputPort : IUseCase<SubmitDeliveryCommand, SubmitDeliveryResult>
{
}
