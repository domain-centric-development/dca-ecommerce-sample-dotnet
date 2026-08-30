using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Inventory.Application.Shared;

/// <summary>Persistence port for <see cref="StockLevel"/> aggregates.</summary>
public interface IStockLevelRepository : IRepository<StockLevel, StockLevelId>
{
    Task<StockLevel?> FindByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockLevel>> FindByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default);
}
