using DcaShop.Inventory.Application.Shared;
using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Inventory.Application.SetStockLevel;

/// <summary>Creates the stock record on first use and corrects it afterwards. Whole use case is local: one short transaction.</summary>
public sealed class SetStockLevelUseCase : ISetStockLevelInputPort
{
    private readonly IStockLevelRepository _stockLevels;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public SetStockLevelUseCase(IStockLevelRepository stockLevels, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _stockLevels = stockLevels;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public Task<SetStockLevelResult> ExecuteAsync(SetStockLevelCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var productId = new ProductId(input.ProductId);

        return _transactionBoundary.InTransactionAsync(async ct =>
        {
            var existing = await _stockLevels.FindByProductIdAsync(productId, ct).ConfigureAwait(false);
            StockLevel stockLevel;
            bool created;
            if (existing is not null)
            {
                stockLevel = existing;
                stockLevel.AdjustStockTo(input.Quantity);
                created = false;
            }
            else
            {
                stockLevel = StockLevel.Create(productId, input.Quantity);
                created = true;
            }

            await _stockLevels.SaveAsync(stockLevel, ct).ConfigureAwait(false);
            await _events.PublishAndClearEventsAsync(stockLevel, ct).ConfigureAwait(false);

            return new SetStockLevelResult(
                stockLevel.Id.Value,
                stockLevel.ProductId.Value,
                stockLevel.AvailableQuantity.Value,
                stockLevel.ReservedQuantity.Value,
                created);
        }, cancellationToken);
    }
}
