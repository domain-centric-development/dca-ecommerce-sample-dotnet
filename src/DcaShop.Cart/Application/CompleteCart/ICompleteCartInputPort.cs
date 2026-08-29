using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.CompleteCart;

public interface ICompleteCartInputPort : IUseCase<CompleteCartCommand, CompleteCartResult>
{
}
