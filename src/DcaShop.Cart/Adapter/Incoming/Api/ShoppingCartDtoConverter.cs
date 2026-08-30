using DcaShop.Cart.Application.GetAllCarts;
using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Adapter.Incoming.Api;

/// <summary>
/// Translates the cart read models into the REST representation. Every cart the API returns is read back through
/// <c>GetCartById</c> after a write, so a single mapping from <see cref="EnrichedCart"/> covers all of them —
/// the client always sees the cart as it now stands, with current article prices.
/// </summary>
public sealed class ShoppingCartDtoConverter
{
    public ShoppingCartDto ToDto(EnrichedCart cart)
    {
        ArgumentNullException.ThrowIfNull(cart);
        var subtotal = cart.CurrentSubtotal;
        return new ShoppingCartDto(
            cart.CartId.Value.ToString(),
            cart.CustomerId.Value,
            cart.Items.Select(ToItemDto).ToList(),
            cart.Status.ToString(),
            subtotal.Amount,
            subtotal.Currency,
            cart.ItemCount);
    }

    public ShoppingCartListDto ToListDto(GetAllCartsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ShoppingCartListDto(
            result.Carts
                .Select(c => new ShoppingCartListDto.CartSummaryDto(
                    c.CartId.ToString(),
                    c.CustomerId,
                    c.Status,
                    c.ItemCount,
                    c.TotalAmount,
                    c.TotalCurrency))
                .ToList());
    }

    private static CartItemDto ToItemDto(EnrichedCartItem item) =>
        new(
            item.Id.Value.ToString(),
            item.ProductId.Value.ToString(),
            item.Quantity.Value,
            item.Article.CurrentPrice.Amount,
            item.Article.CurrentPrice.Currency);
}
