using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Application.Shared;

/// <summary>Current article data (name, price, stock, availability) for products — what the cart needs from the catalog, in the cart's own terms.</summary>
public interface IArticleDataPort : IOutputPort
{
    Task<IReadOnlyDictionary<ProductId, CartArticle>> GetArticleDataAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default);

    Task<CartArticle?> GetArticleDataAsync(ProductId productId, CancellationToken cancellationToken = default);
}
