namespace DcaShop.Inventory.Application.ReleaseStock;

/// <summary>Gives back what an order reserved and that order was then not placed.</summary>
public sealed record ReleaseStockCommand(IReadOnlyList<ReleaseStockCommand.Line> Lines)
{
    /// <summary>How much of which product the order had reserved.</summary>
    public sealed record Line(Guid ProductId, int Quantity);
}