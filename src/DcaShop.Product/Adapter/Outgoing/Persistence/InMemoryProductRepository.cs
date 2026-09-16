using System.Collections.Concurrent;
using DcaShop.Product.Application.Shared;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Product.Adapter.Outgoing.Persistence;

/// <summary>
/// In-memory <see cref="IProductRepository"/>. A concurrent dictionary has no order of its own, so listings sort by
/// product name as the contract requires.
/// </summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<ProductId, Domain.Model.Product> _products = new();

    public Task<Domain.Model.Product?> FindByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.TryGetValue(id, out var product) ? product : null);

    public Task<Domain.Model.Product> SaveAsync(Domain.Model.Product aggregate, CancellationToken cancellationToken = default)
    {
        _products[aggregate.Id] = aggregate;
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        _products.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Domain.Model.Product>> FindAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Domain.Model.Product>>(_products.Values.OrderBy(p => p.Name.Value, StringComparer.Ordinal).ToList());

    public Task<Domain.Model.Product?> FindBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.Values.FirstOrDefault(p => p.Sku == sku));
}
