using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.GetAllCarts;

public interface IGetAllCartsInputPort : IUseCase<GetAllCartsQuery, GetAllCartsResult>
{
}
