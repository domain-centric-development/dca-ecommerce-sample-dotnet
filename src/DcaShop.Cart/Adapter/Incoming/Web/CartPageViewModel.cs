namespace DcaShop.Cart.Adapter.Incoming.Web;

public sealed record CartPageViewModel(Guid CartId, IReadOnlyList<CartPageViewModel.Line> Items, string Subtotal, bool HasPriceChanges, bool CanCheckout)
{
    public sealed record Line(Guid ItemId, Guid ProductId, string Name, string ImageUrl, int Quantity, string UnitPrice, string LineTotal, bool PriceChanged, bool InStock);
}
