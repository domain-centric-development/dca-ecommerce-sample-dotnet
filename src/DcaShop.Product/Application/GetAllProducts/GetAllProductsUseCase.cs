using DcaShop.Product.Application.Shared;

namespace DcaShop.Product.Application.GetAllProducts;

public sealed class GetAllProductsUseCase : IGetAllProductsInputPort
{
    private readonly IProductRepository _products;
    private readonly ProductArticleAssembler _assembler;

    public GetAllProductsUseCase(IProductRepository products, ProductArticleAssembler assembler)
    {
        _products = products;
        _assembler = assembler;
    }

    public async Task<GetAllProductsResult> ExecuteAsync(GetAllProductsQuery query, CancellationToken cancellationToken = default)
    {
        var products = await _products.FindAllAsync(cancellationToken).ConfigureAwait(false);
        var enriched = await _assembler.EnrichAsync(products, cancellationToken).ConfigureAwait(false);
        return new GetAllProductsResult(enriched);
    }
}
