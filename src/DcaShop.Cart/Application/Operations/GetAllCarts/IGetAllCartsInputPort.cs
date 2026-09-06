using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.Operations.GetAllCarts;

public interface IGetAllCartsInputPort : IUseCase<GetAllCartsQuery, GetAllCartsResult>
{
}
