using System.Collections.Concurrent;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Adapter.Outgoing.Persistence;

public sealed class InMemoryShoppingCartRepository : IShoppingCartRepository
{
    private readonly ConcurrentDictionary<CartId, ShoppingCart> _carts = new();

    public Task<ShoppingCart?> FindByIdAsync(CartId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_carts.TryGetValue(id, out var cart) ? cart : null);

    public Task<ShoppingCart> SaveAsync(ShoppingCart aggregate, CancellationToken cancellationToken = default)
    {
        _carts[aggregate.Id] = aggregate;
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(CartId id, CancellationToken cancellationToken = default)
    {
        _carts.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<ShoppingCart?> FindActiveByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_carts.Values.FirstOrDefault(c => c.CustomerId == customerId && c.IsActive));
}
