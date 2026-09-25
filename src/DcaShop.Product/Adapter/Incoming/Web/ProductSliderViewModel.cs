namespace DcaShop.Product.Adapter.Incoming.Web;

public sealed record ProductSliderViewModel(IReadOnlyList<ProductSliderViewModel.Card> Cards)
{
    public sealed record Card(Guid ProductId, string Name, string ImageUrl, string Price);
}