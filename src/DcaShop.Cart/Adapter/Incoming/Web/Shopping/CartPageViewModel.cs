namespace DcaShop.Cart.Adapter.Incoming.Web.Shopping;

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
    /// <summary>The page of a customer who has no active cart: the empty state, nothing to check out.</summary>
    public static CartPageViewModel Empty { get; } = new(Guid.Empty, "Active", [], 0, 0, string.Empty, string.Empty, false, false);

    /// <summary>The success a redirect's notice reported, if any.</summary>
    public string? SuccessMessage { get; init; }

    /// <summary>The refusal a redirect's notice reported, if any.</summary>
    public string? ErrorMessage { get; init; }

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