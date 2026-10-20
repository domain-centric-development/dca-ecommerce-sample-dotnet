using DcaShop.Inventory.Application.Shared;
using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Inventory.Application.ReserveStock;

/// <summary>
/// Reserves the stock of one order before it is placed. Every line is checked against the unreserved stock first and
/// reserved only when all of them are covered, so a refused order earmarks nothing. Runs while no other operation
/// changes the stock of the same products: two orders reserving at once are decided one after the other.
/// </summary>
public sealed class ReserveStockUseCase : IReserveStockInputPort
{
    private readonly IStockLevelRepository _stockLevels;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public ReserveStockUseCase(IStockLevelRepository stockLevels, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _stockLevels = stockLevels;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public Task<ReserveStockResult> ExecuteAsync(ReserveStockCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // A product on two lines is reserved once, for both
        var needed = input.Lines
            .GroupBy(line => new ProductId(line.ProductId))
            .Select(product => (ProductId: product.Key, Quantity: product.Sum(line => line.Quantity)))
            .ToList();

        return _stockLevels.InStockLevelsAsync(
            needed.Select(line => line.ProductId),
            () => _transactionBoundary.InTransactionAsync(ct => ReserveAllOrNoneAsync(needed, ct), cancellationToken),
            cancellationToken);
    }

    /// <summary>Checks every product against its unreserved stock, and reserves them all only when none falls short.</summary>
    private async Task<ReserveStockResult> ReserveAllOrNoneAsync(IReadOnlyList<(ProductId ProductId, int Quantity)> needed, CancellationToken ct)
    {
        var covered = new List<(StockLevel StockLevel, int Quantity)>();
        var shortages = new List<ReserveStockResult.Shortage>();
        foreach (var (productId, quantity) in needed)
        {
            var stockLevel = await _stockLevels.FindByProductIdAsync(productId, ct).ConfigureAwait(false);
            var unreserved = stockLevel?.UnreservedQuantity.Value ?? 0;
            if (stockLevel is null || quantity > unreserved)
            {
                shortages.Add(new ReserveStockResult.Shortage(productId.Value, quantity, unreserved));
            }
            else
            {
                covered.Add((stockLevel, quantity));
            }
        }

        if (shortages.Count > 0)
        {
            return new ReserveStockResult(shortages);
        }

        foreach (var (stockLevel, quantity) in covered)
        {
            stockLevel.Reserve(quantity);
            await _stockLevels.SaveAsync(stockLevel, ct).ConfigureAwait(false);
            await _events.PublishAndClearEventsAsync(stockLevel, ct).ConfigureAwait(false);
        }

        return new ReserveStockResult([]);
    }
}