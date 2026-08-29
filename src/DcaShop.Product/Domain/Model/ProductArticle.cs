using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>The article view of a product: current price, available stock and availability as supplied by Pricing and Inventory.</summary>
public sealed record ProductArticle(Money CurrentPrice, int AvailableStock, bool IsAvailable) : IValue
{
    public bool HasStockFor(int quantity) => IsAvailable && AvailableStock >= quantity;
}
