using DcaShop.Inventory.Application.ProvisionInitialStock;
using DcaShop.Inventory.Events;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.Extensions.Logging;

namespace DcaShop.Inventory.Adapter.Incoming.Event;

/// <summary>
/// Creates the stock record when an integration event implementing <see cref="IStockInitializationTrigger"/>
/// arrives (today: the catalog's product-created event). It hands the event id on; the use case deduplicates a
/// redelivery inside its transaction, so it cannot reset a stock level that changed since.
/// </summary>
public sealed class StockInitializationEventConsumer : EventListener<IStockInitializationTrigger>
{
    private readonly IProvisionInitialStockInputPort _provisionInitialStock;
    private readonly ILogger<StockInitializationEventConsumer> _logger;

    public StockInitializationEventConsumer(IProvisionInitialStockInputPort provisionInitialStock, ILogger<StockInitializationEventConsumer> logger)
    {
        _provisionInitialStock = provisionInitialStock;
        _logger = logger;
    }

    protected override async Task OnAsync(IStockInitializationTrigger @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initialising stock for product {ProductId}", @event.ProductId);
        await _provisionInitialStock.ExecuteAsync(new ProvisionInitialStockCommand(@event.ProductId, @event.InitialStock, @event.EventId), cancellationToken).ConfigureAwait(false);
    }
}