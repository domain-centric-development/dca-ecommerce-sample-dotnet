using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.GetCartById;

public interface IGetCartByIdInputPort : IUseCase<GetCartByIdQuery, GetCartByIdResult>
{
}
