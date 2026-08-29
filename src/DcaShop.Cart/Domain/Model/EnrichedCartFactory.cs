using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>Builds an <see cref="EnrichedCart"/> from a cart and the current article data, one <see cref="CartArticle"/> per product.</summary>
public sealed class EnrichedCartFactory : IFactory
{
    public EnrichedCart Create(ShoppingCart cart, IReadOnlyDictionary<ProductId, CartArticle> articles)
    {
        ArgumentNullException.ThrowIfNull(cart);
        ArgumentNullException.ThrowIfNull(articles);
        var items = new List<EnrichedCartItem>(cart.Items.Count);
        foreach (var item in cart.Items)
        {
            if (!articles.TryGetValue(item.ProductId, out var article))
            {
                throw new ArgumentException($"Missing article data for product {item.ProductId}", nameof(articles));
            }

            items.Add(new EnrichedCartItem(item.Id, item.ProductId, item.Quantity, item.PriceAtAddition, article));
        }

        return new EnrichedCart(cart.Id, cart.CustomerId, cart.Status, items);
    }
}
