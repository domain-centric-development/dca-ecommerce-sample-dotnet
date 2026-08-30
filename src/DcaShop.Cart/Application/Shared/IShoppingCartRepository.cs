using DcaShop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.Shared;

public interface IShoppingCartRepository : IRepository<ShoppingCart, CartId>
{
    /// <summary>
    /// One customer's cart by id — <see langword="null"/> when it does not exist <em>or</em> is not theirs. Every
    /// use case that acts on a cart a caller named reaches for this rather than <see cref="IRepository{T,TId}.FindByIdAsync"/>:
    /// the two cases are indistinguishable on purpose, and a persistence adapter expresses it as one predicate.
    /// <see cref="IRepository{T,TId}.FindByIdAsync"/> stays for the system paths that act on nobody's behalf.
    /// </summary>
    Task<ShoppingCart?> FindByIdForCustomerAsync(CartId id, CustomerId customerId, CancellationToken cancellationToken = default);

    Task<ShoppingCart?> FindActiveByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);

    /// <summary>Every cart, across all customers. Only the operator view has a reason to ask for this.</summary>
    Task<IReadOnlyList<ShoppingCart>> FindAllAsync(CancellationToken cancellationToken = default);
}
