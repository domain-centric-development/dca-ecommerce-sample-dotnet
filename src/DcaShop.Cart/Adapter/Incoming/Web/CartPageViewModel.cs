namespace DcaShop.Cart.Adapter.Incoming.Web;

/// <summary>Everything the cart page renders — the same shape the Java sample's cart view consumes.</summary>
public sealed record CartPageViewModel(
    Guid CartId,
    string Status,
    IReadOnlyList<CartPageViewModel.Line> LineItems,
    int ItemCount,
    int TotalQuantity,
    string CurrentSubtotal,
    string ContainedTax,
    bool HasAnyPriceChanges,
    bool CanCheckout)
{
    public sealed record Line(
        Guid ItemId,
        Guid ProductId,
        string ProductName,
        string ImageUrl,
        int Quantity,
        string UnitPrice,
        string LineTotal,
        bool HasPriceChanged,
        bool PriceIncreased,
        string PriceDifference,
        bool IsAvailable,
        bool HasSufficientStock);
}
