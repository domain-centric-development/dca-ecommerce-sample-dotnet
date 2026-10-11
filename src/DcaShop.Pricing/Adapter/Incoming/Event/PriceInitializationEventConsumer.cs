using DcaShop.Pricing.Application.SetProductPrice;
using DcaShop.Pricing.Application.Shared;
using DcaShop.Pricing.Events;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.Extensions.Logging;

namespace DcaShop.Pricing.Adapter.Incoming.Event;

/// <summary>
/// Creates the price record when an integration event implementing <see cref="IPriceInitializationTrigger"/>
/// arrives (today: the catalog's product-created event). Idempotent: an event id already handled is skipped
/// (<see cref="IProcessedEvents"/>), so an at-least-once redelivery cannot reset a price that changed since.
/// </summary>
public sealed class PriceInitializationEventConsumer : EventListener<IPriceInitializationTrigger>
{
    private readonly ISetProductPriceInputPort _setProductPrice;
    private readonly IProcessedEvents _processedEvents;
    private readonly ILogger<PriceInitializationEventConsumer> _logger;

    public PriceInitializationEventConsumer(ISetProductPriceInputPort setProductPrice, IProcessedEvents processedEvents, ILogger<PriceInitializationEventConsumer> logger)
    {
        _setProductPrice = setProductPrice;
        _processedEvents = processedEvents;
        _logger = logger;
    }

    protected override async Task OnAsync(IPriceInitializationTrigger @event, CancellationToken cancellationToken)
    {
        if (await _processedEvents.ContainsAsync(@event.EventId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Skipping event {EventId}: already handled", @event.EventId);
            return;
        }

        _logger.LogInformation("Initialising price for product {ProductId}", @event.ProductId);
        await _setProductPrice.ExecuteAsync(
            new SetProductPriceCommand(@event.ProductId, decimal.Parse(@event.Amount, System.Globalization.CultureInfo.InvariantCulture), @event.Currency),
            cancellationToken).ConfigureAwait(false);
        await _processedEvents.MarkProcessedAsync(@event.EventId, cancellationToken).ConfigureAwait(false);
    }
}