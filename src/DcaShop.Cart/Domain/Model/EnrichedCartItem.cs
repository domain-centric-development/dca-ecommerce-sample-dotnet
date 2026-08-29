using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>A cart item combined with current article data — enables price comparison, stock checks and line totals.</summary>
public sealed record EnrichedCartItem(CartItemId Id, ProductId ProductId, Quantity Quantity, Price PriceAtAddition, CartArticle Article) : IValue
{
    public Money CurrentLineTotal => Article.CurrentPrice.Multiply(Quantity.Value);

    public Money OriginalLineTotal => PriceAtAddition.Multiply(Quantity.Value);

    public bool HasPriceChanged => Article.CurrentPrice != PriceAtAddition.Value;

    public bool HasSufficientStock => Article.HasStockFor(Quantity.Value);

    public bool IsValidForCheckout => Article.IsAvailable && HasSufficientStock;
}
