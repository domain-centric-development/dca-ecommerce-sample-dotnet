using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;

using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Inventory.Application.Shared;

/// <summary>Persistence port for <see cref="StockLevel"/> aggregates.</summary>
public interface IStockLevelRepository : IRepository<StockLevel, StockLevelId>
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<ProductId, SemaphoreSlim> StockLevelLocks = new();

    /// <summary>
    /// Runs <paramref name="operation"/> while no other operation runs on the stock of these products, so a check of
    /// the stock and the change it allows are never split by another. The products are taken in one order, so two
    /// operations over overlapping products never deadlock: the later one waits for the earlier.
    /// </summary>
    async Task<T> InStockLevelsAsync<T>(IEnumerable<ProductId> productIds, Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var gates = productIds.Distinct().OrderBy(id => id.Value)
            .Select(id => StockLevelLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1)))
            .ToList();
        var taken = new List<SemaphoreSlim>();
        try
        {
            foreach (var gate in gates)
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                taken.Add(gate);
            }

            return await operation().ConfigureAwait(false);
        }
        finally
        {
            taken.ForEach(gate => gate.Release());
        }
    }

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