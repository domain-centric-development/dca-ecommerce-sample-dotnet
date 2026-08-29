using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Product.Application.Shared;

/// <summary>Stock levels from the Inventory context, translated into the catalog's own <see cref="StockData"/>.</summary>
public interface IProductStockDataPort : IOutputPort
{
    Task<IReadOnlyDictionary<ProductId, StockData>> GetStockDataAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default);
}

public sealed record StockData(ProductId ProductId, int AvailableStock, bool IsAvailable);
