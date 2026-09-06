using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.Shopping.CreateCart;

public interface ICreateCartInputPort : IUseCase<CreateCartCommand, CreateCartResult>
{
}
