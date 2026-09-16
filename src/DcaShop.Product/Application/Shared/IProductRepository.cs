using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Product.Application.Shared;

public interface IProductRepository : IRepository<Domain.Model.Product, ProductId>
{
    /// <summary>
    /// All products, ordered by product name (ordinal comparison). The order is part of the contract, so the
    /// catalog reads the same in every persistence profile and in the Java twin.
    /// </summary>
    Task<IReadOnlyList<Domain.Model.Product>> FindAllAsync(CancellationToken cancellationToken = default);

    Task<Domain.Model.Product?> FindBySkuAsync(Domain.Model.Sku sku, CancellationToken cancellationToken = default);
}
