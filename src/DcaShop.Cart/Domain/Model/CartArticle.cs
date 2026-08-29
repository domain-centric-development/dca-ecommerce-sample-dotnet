using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>The cart's own view of an article: name, current price, stock, availability and image — never the catalog's model.</summary>
public sealed record CartArticle(ProductId ProductId, string Name, Money CurrentPrice, int AvailableStock, bool IsAvailable, string ImageUrl) : IValue
{
    public bool HasStockFor(int quantity) => IsAvailable && AvailableStock >= quantity;
}
