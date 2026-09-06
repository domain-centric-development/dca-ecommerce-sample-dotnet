using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;

public interface ISubmitBuyerInfoInputPort : IUseCase<SubmitBuyerInfoCommand, SubmitBuyerInfoResult>
{
}
