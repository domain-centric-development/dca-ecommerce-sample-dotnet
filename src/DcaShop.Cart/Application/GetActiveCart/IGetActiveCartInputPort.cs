using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.GetActiveCart;

public interface IGetActiveCartInputPort : IUseCase<GetActiveCartQuery, GetActiveCartResult>
{
}
