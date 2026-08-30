using DcaShop.Pricing.Application.Shared;
using DcaShop.Pricing.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Pricing.Application.SetProductPrice;

/// <summary>
/// Creates the price record on first use and updates it afterwards. Whole use case is local: one short
/// transaction around load, mutate, save, publish.
/// </summary>
public sealed class SetProductPriceUseCase : ISetProductPriceInputPort
{
    private readonly IProductPriceRepository _prices;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public SetProductPriceUseCase(IProductPriceRepository prices, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _prices = prices;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public Task<SetProductPriceResult> ExecuteAsync(SetProductPriceCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var productId = new ProductId(input.ProductId);
        var newPrice = Money.Of(input.PriceAmount, input.PriceCurrency);

        return _transactionBoundary.InTransactionAsync(async ct =>
        {
            var existing = await _prices.FindByProductIdAsync(productId, ct).ConfigureAwait(false);
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

            return new SetProductPriceResult(
                productPrice.Id.Value,
                productPrice.ProductId.Value,
                productPrice.CurrentPrice.Amount,
                productPrice.CurrentPrice.Currency,
                productPrice.EffectiveFrom,
                created);
        }, cancellationToken);
    }
}
