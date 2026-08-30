namespace DcaShop.Product.Adapter.Incoming.Api;

/// <summary>
/// A product as the API returns it. Price and stock come from the Pricing and Inventory contexts, so both are
/// absent on the answer to a write — a freshly created product has no article data yet.
/// </summary>
public sealed record ProductDto(
    string Id,
    string Sku,
    string Name,
    string Description,
    string ImageUrl,
    decimal? Price,
    string? Currency,
    string Category,
    int? StockQuantity,
    bool? IsAvailable);
