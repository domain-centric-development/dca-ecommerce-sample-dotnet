using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
namespace DcaShop.Cart.Domain.Service;

public sealed class CartPricing : IDomainService
{
    public sealed record Line(ProductId ProductId, Quantity Quantity) : IValue;
    public Money CalculateTotal(IReadOnlyList<Line> lines, IReadOnlyDictionary<ProductId, ArticlePrice> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var total = Money.Euro(0m);
        foreach (var item in lines)
        {
            total = total.Add(facts[item.ProductId].Price.Multiply(item.Quantity.Value));
        }

        return total;

    }

    public CartValidationResult ValidateForCheckout(IReadOnlyList<Line> lines, IReadOnlyDictionary<ProductId, ArticlePrice> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var errors = new List<CartValidationResult.ValidationError>();
        foreach (var item in lines)
        {
            var article = facts.GetValueOrDefault(item.ProductId);
            if (article is null || !article.IsAvailable)
            {
                errors.Add(CartValidationResult.ValidationError.ProductUnavailable(item.ProductId));
            }
            else if (article.AvailableStock < item.Quantity.Value)
            {
                errors.Add(CartValidationResult.ValidationError.InsufficientStock(
                    item.ProductId, item.Quantity.Value, article.AvailableStock));
            }
        }

        return errors.Count == 0 ? CartValidationResult.Valid() : CartValidationResult.WithErrors(errors);

    }
}
