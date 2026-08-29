namespace DcaShop.Product.Application.CreateProduct;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string Description,
    string ImageUrl,
    decimal PriceAmount,
    string PriceCurrency,
    string Category,
    int StockQuantity);
