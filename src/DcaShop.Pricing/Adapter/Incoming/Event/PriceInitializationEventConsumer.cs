using DcaShop.Pricing.Application.SetProductPrice;
using DcaShop.Pricing.Events;
using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace DcaShop.Pricing.Adapter.Incoming.Event;

/// <summary>
/// Creates the price record when an integration event implementing <see cref="IPriceInitializationTrigger"/>
/// arrives (today: the catalog's product-created event). Idempotent: the use case updates an existing record
/// instead of failing, so an at-least-once redelivery is harmless.
/// </summary>
public sealed class PriceInitializationEventConsumer : EventListener<IPriceInitializationTrigger>
{
    private readonly ISetProductPriceInputPort _setProductPrice;
    private readonly ILogger<PriceInitializationEventConsumer> _logger;

    public PriceInitializationEventConsumer(ISetProductPriceInputPort setProductPrice, ILogger<PriceInitializationEventConsumer> logger)
    {
        _setProductPrice = setProductPrice;
        _logger = logger;
    }

    protected override async Task OnAsync(IPriceInitializationTrigger @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initialising price for product {ProductId}", @event.ProductId);
        await _setProductPrice.ExecuteAsync(
            new SetProductPriceCommand(@event.ProductId.Value, @event.InitialPrice.Amount, @event.InitialPrice.Currency),
            cancellationToken).ConfigureAwait(false);
    }
}
