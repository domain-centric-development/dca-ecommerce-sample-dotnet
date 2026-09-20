using System.Collections.Concurrent;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Adapter.Outgoing.Persistence;

public sealed class InMemoryShoppingCartRepository : IShoppingCartRepository
{
    private readonly ConcurrentDictionary<CartId, ShoppingCart> _carts = new();

    /// <summary>The one active cart per customer, held as an index rather than derived by scanning.</summary>
    /// <remarks>
    /// "At most one active cart per customer" is an invariant no single aggregate can hold, so the store holds
    /// it: a claim on this dictionary is what a unique index gives a relational adapter for free. Without it, two
    /// requests that both find no active cart both create one.
    /// </remarks>
    private readonly ConcurrentDictionary<CustomerId, CartId> _activeCartByCustomer = new();

    public Task<ShoppingCart?> FindByIdAsync(CartId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_carts.TryGetValue(id, out var cart) ? cart : null);

    /// <exception cref="InvalidOperationException">
    /// The cart is active and the customer already has a different active cart — the answer a unique index gives,
    /// so a caller written against this adapter works unchanged against a relational one.
    /// </exception>
    public Task<ShoppingCart> SaveAsync(ShoppingCart aggregate, CancellationToken cancellationToken = default)
    {
        if (aggregate.IsActive)
        {
            var claimed = _activeCartByCustomer.GetOrAdd(aggregate.CustomerId, aggregate.Id);
            if (claimed != aggregate.Id)
            {
                throw new InvalidOperationException($"Customer {aggregate.CustomerId} already has an active cart");
            }
        }
        else
        {
            _activeCartByCustomer.TryRemove(new KeyValuePair<CustomerId, CartId>(aggregate.CustomerId, aggregate.Id));
        }

        _carts[aggregate.Id] = aggregate;
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(CartId id, CancellationToken cancellationToken = default)
    {
        if (_carts.TryRemove(id, out var removed))
        {
            _activeCartByCustomer.TryRemove(new KeyValuePair<CustomerId, CartId>(removed.CustomerId, id));
        }

        return Task.CompletedTask;
    }

    public Task<ShoppingCart?> FindByIdForCustomerAsync(CartId id, CustomerId customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_carts.TryGetValue(id, out var cart) && cart.CustomerId == customerId ? cart : null);

    public Task<IReadOnlyList<ShoppingCart>> FindAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ShoppingCart>>(_carts.Values.ToList());

    public Task<ShoppingCart?> FindActiveByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_activeCartByCustomer.TryGetValue(customerId, out var id) && _carts.TryGetValue(id, out var cart) ? cart : null);

    public Task<IReadOnlyList<ShoppingCart>> FindByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ShoppingCart>>(_carts.Values.Where(c => c.CustomerId == customerId).ToList());
}
