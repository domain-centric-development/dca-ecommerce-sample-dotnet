using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Inventory.Domain.Model;

/// <summary>Raised when stock would be decreased by more than the available quantity.</summary>
/// <remarks>
/// The rule is the aggregate's: available quantity never goes negative. Asking for more than there is is a
/// legitimate request with a business answer, not a malformed call — a negative amount would be the malformed
/// one, and that stays an argument guard.
/// </remarks>
public sealed class InsufficientStockException : DomainException
{
    public InsufficientStockException(ProductId productId, int requested, int available)
        : base($"Cannot decrease stock of {productId.Value} by {requested}, only {available} available")
    {
        ProductId = productId;
        Requested = requested;
        Available = available;
    }

    public ProductId ProductId { get; }

    public int Requested { get; }

    public int Available { get; }
}
