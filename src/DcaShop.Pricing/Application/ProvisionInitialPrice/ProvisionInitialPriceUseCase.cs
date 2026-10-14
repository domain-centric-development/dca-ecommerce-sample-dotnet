using DcaShop.Pricing.Application.Shared;
using DcaShop.Pricing.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Pricing.Application.ProvisionInitialPrice;

/// <summary>
/// Creates the price record on first use and updates it afterwards. Whole use case is local: one short
/// transaction around load, mutate, save, publish.
/// </summary>
public sealed class ProvisionInitialPriceUseCase : IProvisionInitialPriceInputPort
{
    /// <summary>The effect's key in <see cref="IProcessedEvents"/> — stable: a rename would replay old events.</summary>
    private const string Effect = "set-product-price";

    private readonly IProductPriceRepository _prices;
    private readonly IProcessedEvents _processedEvents;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public ProvisionInitialPriceUseCase(IProductPriceRepository prices, IProcessedEvents processedEvents, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _prices = prices;
        _processedEvents = processedEvents;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public Task<ProvisionInitialPriceResult> ExecuteAsync(ProvisionInitialPriceCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var productId = new ProductId(input.ProductId);
        var newPrice = Price.Of(Money.Of(input.PriceAmount, input.PriceCurrency));

        return _transactionBoundary.InTransactionAsync(async ct =>
        {
            var existing = await _prices.FindByProductIdAsync(productId, ct).ConfigureAwait(false);
            if (input.EventId is { } eventId && await _processedEvents.ContainsAsync(Effect, eventId, ct).ConfigureAwait(false))
            {
                return existing is null
                    ? new ProvisionInitialPriceResult(Guid.Empty, input.ProductId, 0m, input.PriceCurrency, default, Created: false, AlreadyApplied: true)
                    : ResultOf(existing, created: false, alreadyApplied: true);
            }

            ProductPrice productPrice;
            bool created;
            if (existing is not null)
            {
                productPrice = existing;
                productPrice.UpdatePrice(newPrice);
                created = false;
            }
            else
            {
                productPrice = ProductPrice.Create(productId, newPrice);
                created = true;
            }

            await _prices.SaveAsync(productPrice, ct).ConfigureAwait(false);
            await _events.PublishAndClearEventsAsync(productPrice, ct).ConfigureAwait(false);
            if (input.EventId is { } processed)
            {
                await _processedEvents.MarkProcessedAsync(Effect, processed, ct).ConfigureAwait(false);
            }

            return ResultOf(productPrice, created, alreadyApplied: false);
        }, cancellationToken);
    }

    private static ProvisionInitialPriceResult ResultOf(ProductPrice productPrice, bool created, bool alreadyApplied) =>
        new(
            productPrice.Id.Value,
            productPrice.ProductId.Value,
            productPrice.CurrentPrice.Value.Amount,
            productPrice.CurrentPrice.Value.Currency,
            productPrice.EffectiveFrom,
            created,
            alreadyApplied);
}