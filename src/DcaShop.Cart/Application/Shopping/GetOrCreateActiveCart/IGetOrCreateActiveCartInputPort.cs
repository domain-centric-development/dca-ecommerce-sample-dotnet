using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;

public interface IGetOrCreateActiveCartInputPort : IUseCase<GetOrCreateActiveCartCommand, GetOrCreateActiveCartResult>
{
}
