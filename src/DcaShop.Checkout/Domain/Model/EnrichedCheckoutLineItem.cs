using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>
/// A checkout line item paired with the article's current data, so the domain can answer whether the price
/// moved since the item was added and whether stock still covers the quantity.
/// </summary>
public sealed record EnrichedCheckoutLineItem : IValue
{
    public EnrichedCheckoutLineItem(CheckoutLineItem lineItem, CheckoutArticle currentArticle)
    {
        ArgumentNullException.ThrowIfNull(lineItem);
        ArgumentNullException.ThrowIfNull(currentArticle);
        if (!lineItem.ProductId.Equals(currentArticle.ProductId))
        {
            throw new ArgumentException("Product ID must match between line item and current article", nameof(currentArticle));
        }

        LineItem = lineItem;
        CurrentArticle = currentArticle;
    }

    public CheckoutLineItem LineItem { get; }

    public CheckoutArticle CurrentArticle { get; }

    public Money CurrentLineTotal => CurrentArticle.CurrentPrice.Multiply(LineItem.Quantity);

    public Money OriginalLineTotal => LineItem.LineTotal;

    public bool HasPriceChanged => !CurrentArticle.CurrentPrice.Equals(LineItem.UnitPrice);

    public Money PriceDifference => CurrentArticle.CurrentPrice.IsGreaterThan(LineItem.UnitPrice)
        ? CurrentArticle.CurrentPrice.Subtract(LineItem.UnitPrice)
        : LineItem.UnitPrice.Subtract(CurrentArticle.CurrentPrice);

    public bool HasSufficientStock => CurrentArticle.HasStockFor(LineItem.Quantity);

    public bool IsValidForCheckout => CurrentArticle.IsAvailable && HasSufficientStock;
}
