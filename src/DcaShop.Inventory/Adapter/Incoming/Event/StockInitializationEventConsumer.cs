using DcaShop.Inventory.Application.SetStockLevel;
using DcaShop.Inventory.Application.Shared;
using DcaShop.Inventory.Events;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.Extensions.Logging;

namespace DcaShop.Inventory.Adapter.Incoming.Event;

/// <summary>
/// Creates the stock record when an integration event implementing <see cref="IStockInitializationTrigger"/>
/// arrives (today: the catalog's product-created event). Idempotent: an event id already handled is skipped
/// (<see cref="IProcessedEvents"/>), so an at-least-once redelivery cannot reset a stock level that changed since.
/// </summary>
public sealed class StockInitializationEventConsumer : EventListener<IStockInitializationTrigger>
{
    private readonly ISetStockLevelInputPort _setStockLevel;
    private readonly IProcessedEvents _processedEvents;
    private readonly ILogger<StockInitializationEventConsumer> _logger;

    public StockInitializationEventConsumer(ISetStockLevelInputPort setStockLevel, IProcessedEvents processedEvents, ILogger<StockInitializationEventConsumer> logger)
    {
        _setStockLevel = setStockLevel;
        _processedEvents = processedEvents;
        _logger = logger;
    }

    protected override async Task OnAsync(IStockInitializationTrigger @event, CancellationToken cancellationToken)
    {
        if (await _processedEvents.ContainsAsync(@event.EventId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Skipping event {EventId}: already handled", @event.EventId);
            return;
        }

        _logger.LogInformation("Initialising stock for product {ProductId}", @event.ProductId);
        await _setStockLevel.ExecuteAsync(new SetStockLevelCommand(@event.ProductId, @event.InitialStock), cancellationToken).ConfigureAwait(false);
        await _processedEvents.MarkProcessedAsync(@event.EventId, cancellationToken).ConfigureAwait(false);
    }
}