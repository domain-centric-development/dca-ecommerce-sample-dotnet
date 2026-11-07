using DcaShop.Product.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Product.Application.GetProductById;

public sealed class GetProductByIdUseCase : IGetProductByIdInputPort
{
    private readonly IProductRepository _products;
    private readonly ProductArticleAssembler _assembler;

    public GetProductByIdUseCase(IProductRepository products, ProductArticleAssembler assembler)
    {
        _products = products;
        _assembler = assembler;
    }

    public async Task<GetProductByIdResult> ExecuteAsync(GetProductByIdQuery query, CancellationToken cancellationToken = default)
    {
        var productId = new ProductId(query.ProductId);
        var product = await _products.FindByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return new GetProductByIdResult.ProductNotFound(productId);
        }

        var enriched = await _assembler.EnrichAsync(new[] { product }, cancellationToken).ConfigureAwait(false);
        return new GetProductByIdResult.Found(enriched[0]);
    }
}