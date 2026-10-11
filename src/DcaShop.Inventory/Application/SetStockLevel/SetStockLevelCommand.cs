namespace DcaShop.Inventory.Application.SetStockLevel;

/// <summary>
/// Sets the stock of a product to an absolute figure — creating the record on first use. <paramref name="EventId"/>
/// is the id of the integration event that asked for it; a figure already set for that id is not set again. Without
/// an event id the change is not deduplicated.
/// </summary>
public sealed record SetStockLevelCommand(Guid ProductId, int Quantity, Guid? EventId = null);