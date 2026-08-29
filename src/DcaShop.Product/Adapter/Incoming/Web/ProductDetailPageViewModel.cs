namespace DcaShop.Product.Adapter.Incoming.Web;

public sealed record ProductDetailPageViewModel(
    Guid ProductId,
    string Name,
    string Sku,
    string Description,
    string Category,
    string ImageUrl,
    string Price,
    int AvailableStock,
    bool CanBePurchased);
