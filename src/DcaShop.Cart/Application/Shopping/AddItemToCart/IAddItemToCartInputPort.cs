using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DcaShop.Cart.Application.Shopping.AddItemToCart;

public interface IAddItemToCartInputPort : IUseCase<AddItemToCartCommand, AddItemToCartResult>
{
}
