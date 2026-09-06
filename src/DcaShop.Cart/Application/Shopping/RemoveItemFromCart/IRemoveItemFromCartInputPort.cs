using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.Shopping.RemoveItemFromCart;

public interface IRemoveItemFromCartInputPort : IUseCase<RemoveItemFromCartCommand, RemoveItemFromCartResult>
{
}
