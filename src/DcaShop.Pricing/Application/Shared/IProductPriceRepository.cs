using DcaShop.Pricing.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Pricing.Application.Shared;

/// <summary>Persistence port for <see cref="ProductPrice"/> aggregates.</summary>
public interface IProductPriceRepository : IRepository<ProductPrice, PriceId>
{
    Task<ProductPrice?> FindByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductPrice>> FindByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default);
}
