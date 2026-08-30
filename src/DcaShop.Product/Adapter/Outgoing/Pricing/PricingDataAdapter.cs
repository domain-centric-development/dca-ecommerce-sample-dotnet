using DcaShop.Pricing.Api;
using DcaShop.Product.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Product.Adapter.Outgoing.Pricing;

/// <summary>
/// Anti-corruption layer to the Pricing context: calls its published Api and translates <c>PriceInfo</c> into the
/// catalog's own <see cref="PriceData"/>. A product without a price record is simply absent from the answer.
/// </summary>
public sealed class PricingDataAdapter : IPricingDataPort
{
    private readonly PricingService _pricing;

    public PricingDataAdapter(PricingService pricing)
    {
        _pricing = pricing;
    }

    public async Task<IReadOnlyDictionary<ProductId, PriceData>> GetPricesAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        var prices = await _pricing.GetPricesAsync(productIds, cancellationToken).ConfigureAwait(false);
        return prices.ToDictionary(entry => entry.Key, entry => new PriceData(entry.Value.ProductId, entry.Value.CurrentPrice));
    }
}
