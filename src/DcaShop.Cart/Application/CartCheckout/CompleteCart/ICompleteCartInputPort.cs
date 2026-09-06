using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.CartCheckout.CompleteCart;

public interface ICompleteCartInputPort : IUseCase<CompleteCartCommand, CompleteCartResult>
{
}
