namespace DcaShop.Product.Adapter.Incoming.Web;

public sealed record ProductCatalogPageViewModel(IReadOnlyList<ProductCatalogPageViewModel.Item> Products)
{
    public sealed record Item(Guid ProductId, string Name, string Sku, string Category, string Description, string ImageUrl, string Price, bool CanBePurchased);
}
