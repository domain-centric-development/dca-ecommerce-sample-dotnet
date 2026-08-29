using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Checkout.Application.SubmitBuyerInfo;

public interface ISubmitBuyerInfoInputPort : IUseCase<SubmitBuyerInfoCommand, SubmitBuyerInfoResult>
{
}
