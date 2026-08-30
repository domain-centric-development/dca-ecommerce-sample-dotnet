using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Product;

/// <summary>
/// Product Catalog bounded context: source of truth for product identity (<c>ProductId</c>, <c>SKU</c>)
/// and descriptive master data. Prices and stock levels belong to the Pricing and Inventory contexts; the catalog
/// reads them through their published Apis and translates them into its own article view.
/// </summary>
[BoundedContext("Product Catalog", Description = "Product management and catalog browsing")]
[Upstream("Pricing", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "The catalog shows a price but does not own it; the pricing model is translated into the catalog's own article view")]
[Upstream("Inventory", Translation.AntiCorruptionLayer, Consumes.Api,
    Rationale = "Availability is an inventory statement; the catalog translates it into its own article view")]
[Upstream("Pricing", Translation.Conformist, Consumes.Events,
    Rationale = "ProductCreatedEvent implements pricing's consumer-defined IPriceInitializationTrigger contract as-is")]
[Upstream("Inventory", Translation.Conformist, Consumes.Events,
    Rationale = "ProductCreatedEvent implements inventory's consumer-defined IStockInitializationTrigger contract as-is")]
[Partnership("Pricing",
    Rationale = "The catalog implements pricing's consumer-defined IPriceInitializationTrigger contract; both contexts evolve it together")]
[Partnership("Inventory",
    Rationale = "The catalog implements inventory's consumer-defined IStockInitializationTrigger contract; both contexts evolve it together")]
public static class ProductContext
{
}
