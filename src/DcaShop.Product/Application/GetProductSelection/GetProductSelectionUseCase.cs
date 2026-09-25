using DcaShop.Product.Application.Shared;
using DcaShop.Product.Domain.Model;

namespace DcaShop.Product.Application.GetProductSelection;

/// <summary>
/// Draws the product selection: only products Pricing has a price for are candidates, in catalog order, and the
/// drawn products are enriched and answered in the order they were drawn.
/// </summary>
public sealed class GetProductSelectionUseCase : IGetProductSelectionInputPort
{
    private readonly IProductRepository _products;
    private readonly IPricingDataPort _pricing;
    private readonly ProductArticleAssembler _assembler;
    private readonly Random _random;

    public GetProductSelectionUseCase(IProductRepository products, IPricingDataPort pricing, ProductArticleAssembler assembler, Random random)
    {
        _products = products;
        _pricing = pricing;
        _assembler = assembler;
        _random = random;
    }

    public async Task<GetProductSelectionResult> ExecuteAsync(GetProductSelectionQuery query, CancellationToken cancellationToken = default)
    {
        var catalog = await _products.FindAllAsync(cancellationToken).ConfigureAwait(false);
        var prices = await _pricing.GetPricesAsync(catalog.Select(p => p.Id).ToArray(), cancellationToken).ConfigureAwait(false);
        var priced = catalog.Where(p => prices.ContainsKey(p.Id)).Select(p => p.Id).ToList();

        var selection = ProductSelection.Draw(priced, _random);

        var byId = catalog.ToDictionary(p => p.Id);
        var drawn = selection.ProductIds.Select(id => byId[id]).ToList();
        var enriched = await _assembler.EnrichAsync(drawn, cancellationToken).ConfigureAwait(false);
        return new GetProductSelectionResult(enriched);
    }
}