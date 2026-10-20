using DcaShop.Inventory.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Inventory.Application.ReleaseStock;

/// <summary>
/// Gives back the reservation of an order that was then not placed. Never more than is reserved: a product whose
/// reservation is already gone releases what is left of it, and one without a stock level releases nothing.
/// </summary>
public sealed class ReleaseStockUseCase : IReleaseStockInputPort
{
    private readonly IStockLevelRepository _stockLevels;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public ReleaseStockUseCase(IStockLevelRepository stockLevels, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _stockLevels = stockLevels;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public Task<ReleaseStockResult> ExecuteAsync(ReleaseStockCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return _stockLevels.InStockLevelsAsync(
            input.Lines.Select(line => new ProductId(line.ProductId)),
            () => _transactionBoundary.InTransactionAsync(ct => ReleaseReservedAsync(input.Lines, ct), cancellationToken),
            cancellationToken);
    }

    /// <summary>Releases each line's reservation, as far as one is left, and answers the products released.</summary>
    private async Task<ReleaseStockResult> ReleaseReservedAsync(IReadOnlyList<ReleaseStockCommand.Line> lines, CancellationToken ct)
    {
        var released = new List<Guid>();
        foreach (var line in lines)
        {
            var stockLevel = await _stockLevels.FindByProductIdAsync(new ProductId(line.ProductId), ct).ConfigureAwait(false);
            var amount = stockLevel is null ? 0 : Math.Min(line.Quantity, stockLevel.ReservedQuantity.Value);
            if (stockLevel is null || amount == 0)
            {
                continue;
            }

            stockLevel.Release(amount);
            await _stockLevels.SaveAsync(stockLevel, ct).ConfigureAwait(false);
            await _events.PublishAndClearEventsAsync(stockLevel, ct).ConfigureAwait(false);
            released.Add(line.ProductId);
        }

        return new ReleaseStockResult(released);
    }
}