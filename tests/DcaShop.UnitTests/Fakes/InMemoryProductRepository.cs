using System.Collections.Concurrent;

using DcaShop.Product.Application.Shared;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Product.Adapter.Outgoing.Persistence;

/// <summary>
/// In-memory <see cref="IProductRepository"/> — the unit tests' stand-in for the database adapter, storing and handing
/// out copies as a database does. A concurrent dictionary has no order of its own, so listings sort by product name as
/// the contract requires.
/// </summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<ProductId, Domain.Model.Product> _products = new();

    /// <summary>The product a SKU names, claimed on save the way a unique column claims it.</summary>
    private readonly ConcurrentDictionary<Sku, ProductId> _bySku = new();

    public Task<Domain.Model.Product?> FindByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.TryGetValue(id, out var product) ? Copy(product) : null);

    /// <exception cref="InvalidOperationException">Another product already carries this SKU.</exception>
    public Task<Domain.Model.Product> SaveAsync(Domain.Model.Product aggregate, CancellationToken cancellationToken = default)
    {
        var claimed = _bySku.GetOrAdd(aggregate.Sku, aggregate.Id);
        if (claimed != aggregate.Id)
        {
            throw new InvalidOperationException($"SKU {aggregate.Sku} is already taken");
        }

        // A product that changed its SKU must stop resolving under the old one
        foreach (var stale in _bySku.Where(e => e.Value == aggregate.Id && e.Key != aggregate.Sku).ToList())
        {
            _bySku.TryRemove(stale);
        }

        _products[aggregate.Id] = Copy(aggregate);
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        if (_products.TryRemove(id, out var removed))
        {
            _bySku.TryRemove(new KeyValuePair<Sku, ProductId>(removed.Sku, id));
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Domain.Model.Product>> FindAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Domain.Model.Product>>(_products.Values.OrderBy(p => p.Name.Value, StringComparer.Ordinal).Select(Copy).ToList());

    public Task<Domain.Model.Product?> FindBySkuAsync(Sku sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(_bySku.TryGetValue(sku, out var id) && _products.TryGetValue(id, out var product) ? Copy(product) : null);

    private static Domain.Model.Product Copy(Domain.Model.Product product) => Domain.Model.Product.Reconstitute(
        product.Id, product.Sku, product.Name, product.Description, product.Category, product.ImageUrl);
}