using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Resolves current price and availability for an article; the use case builds it from freshly fetched article data and passes it in.</summary>
public interface ICheckoutArticlePriceResolver
{
    ArticlePrice Resolve(ProductId productId);
}

public sealed record ArticlePrice(Money Price, bool IsAvailable, int AvailableStock) : IValue;
