using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Product.Application.Shared;

public interface IProductRepository : IRepository<Domain.Model.Product, ProductId>
{
    Task<IReadOnlyList<Domain.Model.Product>> FindAllAsync(CancellationToken cancellationToken = default);

    Task<Domain.Model.Product?> FindBySkuAsync(Domain.Model.Sku sku, CancellationToken cancellationToken = default);
}
