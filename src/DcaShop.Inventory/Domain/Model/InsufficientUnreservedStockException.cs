using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Inventory.Domain.Model;

/// <summary>Raised when more stock would be reserved than is unreserved.</summary>
/// <remarks>
/// Unreserved quantity is available minus reserved — what can still be promised. The aggregate's invariant is
/// that reservations never exceed the available quantity, so a reservation beyond the unreserved part is
/// refused.
/// </remarks>
public sealed class InsufficientUnreservedStockException : DomainException
{
    public InsufficientUnreservedStockException(ProductId productId, int requested, int unreserved)
        : base($"Cannot reserve {requested} of {productId.Value}, only {unreserved} unreserved")
    {
        ProductId = productId;
        Requested = requested;
        Unreserved = unreserved;
    }

    public ProductId ProductId { get; }

    public int Requested { get; }

    public int Unreserved { get; }
}