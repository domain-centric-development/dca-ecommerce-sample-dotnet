using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Inventory.Application.Shared;

/// <summary>Persistence port for <see cref="StockLevel"/> aggregates.</summary>
public interface IStockLevelRepository : IRepository<StockLevel, StockLevelId>
{
    Task<StockLevel?> FindByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockLevel>> FindByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default);

    /// <summary>Every stock level. Only an operator view has a reason to ask for this.</summary>
    Task<IReadOnlyList<StockLevel>> FindAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The stock levels matching a specification.
    /// </summary>
    /// <remarks>
    /// The specification is stated in domain terms; a persistence adapter translates it into native predicates
    /// (see <see cref="ICompositeSpecification{T}.Accept{TResult}"/>) so filtering happens where the data is.
    /// This default filters in memory, so an adapter can adopt push-down step by step.
    /// </remarks>
    async Task<IReadOnlyList<StockLevel>> FindByAsync(
        ICompositeSpecification<StockLevel> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var all = await FindAllAsync(cancellationToken).ConfigureAwait(false);
        return all.Where(specification.IsSatisfiedBy).ToList();
    }
}
