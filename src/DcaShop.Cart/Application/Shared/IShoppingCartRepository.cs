using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;
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

    /// <summary>Every cart of one customer, whatever its status.</summary>
    Task<IReadOnlyList<ShoppingCart>> FindByCustomerAsync(CustomerId customerId, CancellationToken cancellationToken = default);

    /// <summary>Every cart, across all customers. Only the operator view has a reason to ask for this.</summary>
    Task<IReadOnlyList<ShoppingCart>> FindAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The carts matching a specification, one page at a time.
    /// </summary>
    /// <remarks>
    /// The specification is stated in domain terms; a persistence adapter translates it into native predicates
    /// (see <see cref="ICompositeSpecification{T}.Accept{TResult}"/>) so filtering and paging happen where the
    /// data is. This default filters and pages in memory, so an adapter can adopt push-down step by step.
    /// </remarks>
    async Task<PageResult<ShoppingCart>> FindByAsync(
        ICompositeSpecification<ShoppingCart> specification,
        PagingRequest paging,
        CancellationToken cancellationToken = default)
    {
        var all = await FindAllAsync(cancellationToken).ConfigureAwait(false);
        var matching = all.Where(specification.IsSatisfiedBy).ToList();
        var content = matching.Skip((int)paging.Offset).Take(paging.PageSize).ToList();
        return new PageResult<ShoppingCart>(content, matching.Count, paging.PageNumber, paging.PageSize);
    }
}
