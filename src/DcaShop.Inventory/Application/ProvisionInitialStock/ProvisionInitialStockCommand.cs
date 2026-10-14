namespace DcaShop.Inventory.Application.ProvisionInitialStock;

/// <summary>
/// Provisions the initial stock of a product as an absolute figure — creating the record on first use. <paramref name="EventId"/>
/// is the id of the integration event that asked for it; a figure already set for that id is not set again. Without
/// an event id the change is not deduplicated.
/// </summary>
public sealed record ProvisionInitialStockCommand(Guid ProductId, int Quantity, Guid? EventId = null);