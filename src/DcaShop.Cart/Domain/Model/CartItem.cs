using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>A single item in the cart: product reference, quantity and the price captured when it was added.</summary>
public sealed class CartItem : IEntity<CartItem, CartItemId>
{
    internal CartItem(CartItemId id, ProductId productId, Quantity quantity, Price priceAtAddition)
    {
        Id = id;
        ProductId = productId;
        Quantity = quantity;
        PriceAtAddition = priceAtAddition ?? throw new ArgumentNullException(nameof(priceAtAddition));
    }

    public CartItemId Id { get; }

    public ProductId ProductId { get; }

    public Quantity Quantity { get; private set; }

    public Price PriceAtAddition { get; }

    public Money LineTotal => PriceAtAddition.Multiply(Quantity.Value);

    internal void UpdateQuantity(Quantity newQuantity) => Quantity = newQuantity;

    internal void IncreaseQuantity() => Quantity = Quantity.Increase();

    internal void DecreaseQuantity() => Quantity = Quantity.Decrease();
}
