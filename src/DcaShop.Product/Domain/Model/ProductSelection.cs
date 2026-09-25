using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>Up to <see cref="MaxSize"/> different products drawn at random from the priced candidates.</summary>
public sealed record ProductSelection : IValue
{
    public const int MaxSize = 8;

    private ProductSelection(IReadOnlyList<ProductId> productIds)
    {
        ProductIds = productIds;
    }

    /// <summary>The drawn products, in the order they were drawn.</summary>
    public IReadOnlyList<ProductId> ProductIds { get; }

    /// <summary>
    /// Draws up to <see cref="MaxSize"/> different products from <paramref name="pricedProducts"/>; every candidate
    /// when there are fewer, none when there are none. The draw depends only on the candidates and the random source.
    /// </summary>
    public static ProductSelection Draw(IReadOnlyCollection<ProductId> pricedProducts, Random random)
    {
        ArgumentNullException.ThrowIfNull(pricedProducts);
        ArgumentNullException.ThrowIfNull(random);

        var pool = pricedProducts.Distinct().ToArray();
        var size = Math.Min(MaxSize, pool.Length);
        for (var i = 0; i < size; i++)
        {
            var pick = random.Next(i, pool.Length);
            (pool[i], pool[pick]) = (pool[pick], pool[i]);
        }

        return new ProductSelection(pool.Take(size).ToList());
    }

    public bool Equals(ProductSelection? other) => other is not null && ProductIds.SequenceEqual(other.ProductIds);

    public override int GetHashCode() => ProductIds.Aggregate(0, HashCode.Combine);
}