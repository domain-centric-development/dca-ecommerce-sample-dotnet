using DomainCentric.BuildingBlocks.Ddd.Strategic;

namespace DcaShop.Product;

/// <summary>
/// Product Catalog bounded context: source of truth for product identity (<c>ProductId</c>, <c>SKU</c>)
/// and descriptive master data. Prices and stock levels belong to the Pricing and Inventory contexts;
/// until those exist in this sample they are answered by in-memory stand-in adapters behind the same ports.
/// </summary>
[BoundedContext("Product Catalog", Description = "Product management and catalog browsing")]
public static class ProductContext
{
}
