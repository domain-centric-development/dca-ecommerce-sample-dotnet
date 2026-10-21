using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;
namespace DcaShop.Checkout.Domain.Service;

/// <summary>
/// Judges whether a session's lines can still be bought, against the article facts of this moment: available, and in
/// stock for the quantity. A price that moved is no reason to refuse here — whether the customer saw it is the order
/// fingerprint's to say.
/// </summary>
public sealed class CheckoutPricing : IDomainService
{
    public CheckoutValidationResult ValidateItems(CheckoutSession session, IReadOnlyDictionary<ProductId, ArticlePrice> facts)
    {
        var lines = session.LineItems;
        ArgumentNullException.ThrowIfNull(facts);
        var errors = new List<ValidationError>();
        foreach (var item in lines)
        {
            var article = facts.GetValueOrDefault(item.ProductId);
            if (article is null || !article.IsAvailable)
            {
                errors.Add(ValidationError.ProductUnavailable(item.ProductId, item.ProductName));
            }
            else if (article.AvailableStock < item.Quantity)
            {
                errors.Add(ValidationError.InsufficientStock(item.ProductId, item.Quantity, article.AvailableStock, item.ProductName));
            }
        }

        return errors.Count == 0 ? CheckoutValidationResult.Valid() : CheckoutValidationResult.WithErrors(errors);

    }
}