using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.Shopping.GetActiveCart;

public interface IGetActiveCartInputPort : IUseCase<GetActiveCartQuery, GetActiveCartResult>
{
}
