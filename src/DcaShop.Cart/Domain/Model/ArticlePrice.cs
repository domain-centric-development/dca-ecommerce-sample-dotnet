using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>
/// Current price and availability of an article, in the cart's own terms.
/// </summary>
/// <remarks>
/// This is what the cart domain needs to know about an article at settlement time; it lets the aggregate work
/// with fresh figures without knowing where they came from.
/// </remarks>
public sealed record ArticlePrice : IValue
{
    public ArticlePrice(Money price, bool isAvailable, int availableStock)
    {
        if (availableStock < 0)
        {
            throw new ArgumentException("Available stock cannot be negative", nameof(availableStock));
        }

        Price = price ?? throw new ArgumentNullException(nameof(price));
        IsAvailable = isAvailable;
        AvailableStock = availableStock;
    }

    public Money Price { get; }

    public bool IsAvailable { get; }

    public int AvailableStock { get; }
}
