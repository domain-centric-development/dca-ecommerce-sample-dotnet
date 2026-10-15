using DcaShop.Pricing.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Pricing.Application.ChangePrice;

/// <summary>
/// Replaces the current price of a product. Whole use case is local: one short transaction around load, mutate, save,
/// publish. A product Pricing holds no price for is refused with <see cref="ProductPriceNotFoundException"/>.
/// </summary>
public sealed class ChangePriceUseCase : IChangePriceInputPort
{
    private readonly IProductPriceRepository _prices;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public ChangePriceUseCase(IProductPriceRepository prices, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _prices = prices;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public Task<ChangePriceResult> ExecuteAsync(ChangePriceCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var productId = new ProductId(input.ProductId);
        var newPrice = Price.Of(Money.Of(input.PriceAmount, Currency.Of(input.PriceCurrency)));

        return _transactionBoundary.InTransactionAsync(async ct =>
        {
            var productPrice = await _prices.FindByProductIdAsync(productId, ct).ConfigureAwait(false)
                ?? throw new ProductPriceNotFoundException(productId);

            productPrice.UpdatePrice(newPrice);
            await _prices.SaveAsync(productPrice, ct).ConfigureAwait(false);
            await _events.PublishAndClearEventsAsync(productPrice, ct).ConfigureAwait(false);

            return new ChangePriceResult(
                productPrice.ProductId.Value,
                productPrice.CurrentPrice.Value.Amount,
                productPrice.CurrentPrice.Value.Currency.Code,
                productPrice.EffectiveFrom);
        }, cancellationToken);
    }
}