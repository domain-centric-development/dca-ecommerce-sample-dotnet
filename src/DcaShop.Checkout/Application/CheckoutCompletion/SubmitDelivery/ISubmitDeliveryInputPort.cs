using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitDelivery;

public interface ISubmitDeliveryInputPort : IUseCase<SubmitDeliveryCommand, SubmitDeliveryResult>
{
}
