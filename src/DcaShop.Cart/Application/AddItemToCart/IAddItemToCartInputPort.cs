using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.AddItemToCart;

public interface IAddItemToCartInputPort : IUseCase<AddItemToCartCommand, AddItemToCartResult>
{
}
