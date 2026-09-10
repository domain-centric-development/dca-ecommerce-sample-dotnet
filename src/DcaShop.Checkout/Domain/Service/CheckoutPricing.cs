using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
namespace DcaShop.Checkout.Domain.Service;

public sealed class CheckoutPricing : IDomainService
{
    public Money CalculateOrderTotal(IReadOnlyList<CheckoutLineItem> lines, IReadOnlyDictionary<ProductId, ArticlePrice> facts, string currency)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var total = Money.Zero(currency);
        foreach (var item in lines)
        {
            total = total.Add(facts[item.ProductId].Price.Multiply(item.Quantity));
        }

        return total;

    }

    public CheckoutValidationResult ValidateItems(IReadOnlyList<CheckoutLineItem> lines, IReadOnlyDictionary<ProductId, ArticlePrice> facts, string currency)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var errors = new List<ValidationError>();
        foreach (var item in lines)
        {
            var article = facts.GetValueOrDefault(item.ProductId);
            if (article is null || !article.IsAvailable)
            {
                errors.Add(ValidationError.ProductUnavailable(item.ProductId));
            }
            else if (article.Price != item.UnitPrice)
            {
                errors.Add(new ValidationError(item.ProductId, ErrorType.PriceChanged, $"Product {item.ProductId}: price changed; start a fresh checkout to accept it"));
            }
            else if (article.AvailableStock < item.Quantity)
            {
                errors.Add(ValidationError.InsufficientStock(item.ProductId, item.Quantity, article.AvailableStock));
            }
        }

        return errors.Count == 0 ? CheckoutValidationResult.Valid() : CheckoutValidationResult.WithErrors(errors);

    }
}
