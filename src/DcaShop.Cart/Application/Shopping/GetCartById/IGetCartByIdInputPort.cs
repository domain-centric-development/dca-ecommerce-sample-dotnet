using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.Shopping.GetCartById;

public interface IGetCartByIdInputPort : IUseCase<GetCartByIdQuery, GetCartByIdResult>
{
}
