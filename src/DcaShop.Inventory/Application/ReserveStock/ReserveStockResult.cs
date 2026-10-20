namespace DcaShop.Inventory.Application.ReserveStock;

/// <summary>
/// Outcome of a reservation: reserved when no product falls short; otherwise the products the unreserved stock does
/// not cover, in the order asked, and nothing was reserved.
/// </summary>
public sealed record ReserveStockResult(IReadOnlyList<ReserveStockResult.Shortage> Shortages)
{
    /// <summary>Whether every line was reserved.</summary>
    public bool Reserved => Shortages.Count == 0;

    /// <summary>A product of which <paramref name="Requested"/> was asked and only <paramref name="Unreserved"/> is left to promise.</summary>
    public sealed record Shortage(Guid ProductId, int Requested, int Unreserved);
}