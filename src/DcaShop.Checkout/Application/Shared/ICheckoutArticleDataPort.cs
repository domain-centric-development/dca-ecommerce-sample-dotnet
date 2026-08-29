using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Current article data (name, price, availability, stock) for the products in a checkout.</summary>
public interface ICheckoutArticleDataPort : IOutputPort
{
    Task<IReadOnlyDictionary<ProductId, CheckoutArticle>> GetArticleDataAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default);
}
