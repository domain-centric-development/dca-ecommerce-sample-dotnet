using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.CartOversight.GetAllCarts;

public interface IGetAllCartsInputPort : IUseCase<GetAllCartsQuery, GetAllCartsResult>
{
}