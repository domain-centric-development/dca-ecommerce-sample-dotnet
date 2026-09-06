using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.SubmitPayment;

public interface ISubmitPaymentInputPort : IUseCase<SubmitPaymentCommand, SubmitPaymentResult>
{
}
