using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.Shopping.RemoveCartItem;

public interface IRemoveCartItemInputPort : IUseCase<RemoveCartItemCommand, RemoveCartItemResult>
{
}