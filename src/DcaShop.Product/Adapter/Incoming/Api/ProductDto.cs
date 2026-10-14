namespace DcaShop.Product.Adapter.Incoming.Api;

/// <summary>
/// A product as the API returns it. Price and stock come from the Pricing and Inventory contexts. The answer to a
/// create echoes the price and currency the product was created with; stock and availability stay absent there,
/// because a freshly created product has no stock figures yet.
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