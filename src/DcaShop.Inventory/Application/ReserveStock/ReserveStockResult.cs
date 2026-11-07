using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Inventory.Application.ReserveStock;

/// <summary>
/// What a reservation came to: every line reserved, or the unreserved stock does not cover some products — then
/// nothing was reserved.
/// </summary>
[GenerateOneOf]
public sealed partial class ReserveStockResult
    : OneOfBase<ReserveStockResult.Reserved, ReserveStockResult.InsufficientUnreservedStock>
{
    /// <summary>Every line was reserved.</summary>
    public sealed record Reserved;

    /// <summary>
    /// The unreserved stock does not cover the products named, in the order asked; nothing was reserved.
    /// </summary>
    public sealed record InsufficientUnreservedStock(IReadOnlyList<Shortage> Shortages) : IRefusal;

    /// <summary>A product of which <paramref name="Requested"/> was asked and only <paramref name="Unreserved"/> is left to promise.</summary>
    public sealed record Shortage(Guid ProductId, int Requested, int Unreserved);
}