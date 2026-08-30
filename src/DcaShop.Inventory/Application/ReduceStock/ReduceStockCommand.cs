namespace DcaShop.Inventory.Application.ReduceStock;

/// <summary>Takes a quantity out of stock — a sold or shipped item.</summary>
public sealed record ReduceStockCommand(Guid ProductId, int Quantity);
