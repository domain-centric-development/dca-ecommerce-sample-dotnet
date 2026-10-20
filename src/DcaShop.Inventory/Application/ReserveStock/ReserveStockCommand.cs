namespace DcaShop.Inventory.Application.ReserveStock;

/// <summary>Earmarks the quantities of one order out of the unreserved stock — every line, or none.</summary>
public sealed record ReserveStockCommand(IReadOnlyList<ReserveStockCommand.Line> Lines)
{
    /// <summary>How much of which product the order needs.</summary>
    public sealed record Line(Guid ProductId, int Quantity);
}