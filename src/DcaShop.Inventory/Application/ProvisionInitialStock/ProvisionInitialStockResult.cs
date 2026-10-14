namespace DcaShop.Inventory.Application.ProvisionInitialStock;

/// <summary>The stock record after the call; <paramref name="AlreadyApplied"/> when the event had set it before and nothing changed.</summary>
public sealed record ProvisionInitialStockResult(Guid StockLevelId, Guid ProductId, int AvailableQuantity, int ReservedQuantity, bool Created, bool AlreadyApplied = false);