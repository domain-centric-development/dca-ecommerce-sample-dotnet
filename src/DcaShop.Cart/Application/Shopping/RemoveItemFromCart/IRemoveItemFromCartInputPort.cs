using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.RemoveItemFromCart;

public interface IRemoveItemFromCartInputPort : IUseCase<RemoveItemFromCartCommand, RemoveItemFromCartResult>
{
}
