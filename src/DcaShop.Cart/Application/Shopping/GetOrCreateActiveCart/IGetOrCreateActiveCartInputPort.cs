using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.GetOrCreateActiveCart;

public interface IGetOrCreateActiveCartInputPort : IUseCase<GetOrCreateActiveCartCommand, GetOrCreateActiveCartResult>
{
}
