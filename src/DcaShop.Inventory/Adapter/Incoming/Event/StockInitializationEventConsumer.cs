using DcaShop.Inventory.Application.SetStockLevel;
using DcaShop.Inventory.Events;
using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace DcaShop.Inventory.Adapter.Incoming.Event;

/// <summary>
/// Creates the stock record when an integration event implementing <see cref="IStockInitializationTrigger"/>
/// arrives (today: the catalog's product-created event). Idempotent: setting the same figure twice is a no-op
/// in effect, so an at-least-once redelivery is harmless.
/// </summary>
public sealed class StockInitializationEventConsumer : EventListener<IStockInitializationTrigger>
{
    private readonly ISetStockLevelInputPort _setStockLevel;
    private readonly ILogger<StockInitializationEventConsumer> _logger;

    public StockInitializationEventConsumer(ISetStockLevelInputPort setStockLevel, ILogger<StockInitializationEventConsumer> logger)
    {
        _setStockLevel = setStockLevel;
        _logger = logger;
    }

    protected override async Task OnAsync(IStockInitializationTrigger @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initialising stock for product {ProductId}", @event.ProductId);
        await _setStockLevel.ExecuteAsync(new SetStockLevelCommand(@event.ProductId.Value, @event.InitialStock), cancellationToken).ConfigureAwait(false);
    }
}
