namespace DcaShop.Inventory.Application.ReduceStock;

/// <summary>
/// Takes quantities out of stock — the lines of one sale, reduced together. <paramref name="EventId"/> is the id of
/// the integration event that reported the sale; a reduction already made for that id is not made again. Without an
/// event id the reduction is not deduplicated.
/// </summary>
public sealed record ReduceStockCommand(IReadOnlyList<ReduceStockCommand.Line> Lines, Guid? EventId = null)
{
    /// <summary>How much of which product left the shop.</summary>
    public sealed record Line(Guid ProductId, int Quantity);
}