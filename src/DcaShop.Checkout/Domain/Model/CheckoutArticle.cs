using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Checkout's own view of an article: current price, availability and stock, translated from other contexts.</summary>
public sealed record CheckoutArticle(ProductId ProductId, string Name, Money CurrentPrice, bool IsAvailable, int AvailableStock, string? ImageUrl) : IValue
{
    public bool HasStockFor(int quantity) => IsAvailable && AvailableStock >= quantity;
}
