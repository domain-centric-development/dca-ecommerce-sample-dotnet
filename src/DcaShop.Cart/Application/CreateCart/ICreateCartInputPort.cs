using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.CreateCart;

public interface ICreateCartInputPort : IUseCase<CreateCartCommand, CreateCartResult>
{
}
