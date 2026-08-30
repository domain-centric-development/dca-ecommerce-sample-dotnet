using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>
/// Assembles a <see cref="CheckoutCart"/> from line items and the article data fetched for them. It never
/// fetches anything itself — the application layer passes the data in.
/// </summary>
public sealed class CheckoutCartFactory : IFactory
{
    public CheckoutCart Create(
        CartId cartId,
        CustomerId customerId,
        IReadOnlyList<CheckoutLineItem> lineItems,
        IReadOnlyDictionary<ProductId, CheckoutArticle> articleData)
    {
        ArgumentNullException.ThrowIfNull(lineItems);
        ArgumentNullException.ThrowIfNull(articleData);

        var missing = lineItems.Where(i => !articleData.ContainsKey(i.ProductId)).Select(i => i.ProductId).ToList();
        if (missing.Count > 0)
        {
            throw new ArgumentException($"Missing article data for product IDs: {string.Join(", ", missing)}", nameof(articleData));
        }

        var enriched = lineItems.Select(i => new EnrichedCheckoutLineItem(i, articleData[i.ProductId])).ToList();
        return new CheckoutCart(cartId, customerId, enriched);
    }

    /// <summary>Convenience for an existing session: takes its cart, customer and line items.</summary>
    public CheckoutCart FromSession(CheckoutSession session, IReadOnlyDictionary<ProductId, CheckoutArticle> articleData)
    {
        ArgumentNullException.ThrowIfNull(session);
        return Create(session.CartId, session.CustomerId, session.LineItems, articleData);
    }
}
