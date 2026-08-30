namespace DcaShop.Inventory.Application.SetStockLevel;

/// <summary>Sets the stock of a product to an absolute figure — creating the record on first use.</summary>
public sealed record SetStockLevelCommand(Guid ProductId, int Quantity);
