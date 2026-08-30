namespace DcaShop.Inventory.Application.SetStockLevel;

public sealed record SetStockLevelResult(Guid StockLevelId, Guid ProductId, int AvailableQuantity, int ReservedQuantity, bool Created);
