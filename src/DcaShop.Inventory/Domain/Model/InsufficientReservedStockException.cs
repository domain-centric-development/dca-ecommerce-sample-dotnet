using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Inventory.Domain.Model;

/// <summary>Raised when more stock would be released than is reserved.</summary>
/// <remarks>
/// Releasing returns an earmarked quantity to the promisable pool; releasing more than was ever reserved would
/// invent stock the warehouse does not have.
/// </remarks>
public sealed class InsufficientReservedStockException : DomainException
{
    public InsufficientReservedStockException(ProductId productId, int requested, int reserved)
        : base($"Cannot release {requested} of {productId.Value}, only {reserved} reserved")
    {
        ProductId = productId;
        Requested = requested;
        Reserved = reserved;
    }

    public ProductId ProductId { get; }

    public int Requested { get; }

    public int Reserved { get; }
}
