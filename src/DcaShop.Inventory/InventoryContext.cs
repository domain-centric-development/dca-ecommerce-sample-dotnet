using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Inventory;

/// <summary>
/// Inventory bounded context: how much of a product is on hand and how much of it is spoken for. It depends on no
/// other context — stock records are created and reduced through the consumer-defined triggers in
/// <c>DcaShop.Inventory.Events</c>, and read back through the published <see cref="Api.InventoryService"/>.
/// </summary>
[BoundedContext("Inventory", Description = "Stock level management and availability tracking")]
[Partnership("Checkout",
    Rationale = "Inventory owns the consumer-defined IStockReductionTrigger contract that checkout events implement; both contexts evolve it together")]
[Partnership("Product",
    Rationale = "Inventory owns the consumer-defined IStockInitializationTrigger contract that catalog events implement; both contexts evolve it together")]
public static class InventoryContext
{
}
