using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.Shared;

public interface IShoppingCartRepository : IRepository<ShoppingCart, CartId>
{
    Task<ShoppingCart?> FindActiveByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);
}
