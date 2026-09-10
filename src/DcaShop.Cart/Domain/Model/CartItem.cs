using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>A single item in the cart: product reference, quantity and the price captured when it was added.</summary>
public sealed class CartItem : IEntity<CartItem, CartItemId>
{
    private PositionUnits _units;
    internal CartItem(CartItemId id, ProductId productId, Quantity quantity, Price priceAtAddition)
    {
        if (quantity.Value <= 0) throw new ArgumentException("Quantity must be positive", nameof(quantity));
        Id = id;
        ProductId = productId;
        Quantity = quantity;
        _units = PositionUnits.Initial(quantity.Value);
        PriceAtAddition = priceAtAddition ?? throw new ArgumentNullException(nameof(priceAtAddition));
    }

    public string PositionSnapshot => Id.Value + ":" + _units.Serialize();
    public string StoredUnits => _units.Serialize();
    internal void RestoreUnits(string encoded)
    {
        var restored = PositionUnits.Parse(encoded);
        if (restored.Quantity != Quantity.Value) throw new ArgumentException("Stored unit count mismatch");
        _units = restored;
    }
    internal bool Reconcile(PositionUnits purchased)
    {
        var remaining = _units.Reconcile(purchased);
        if (remaining.Quantity == _units.Quantity) return false;
        _units = remaining;
        if (_units.Quantity > 0) Quantity = Quantity.Of(_units.Quantity);
        return true;
    }
    internal bool HasUnits => _units.Quantity > 0;

    public CartItemId Id { get; }

    public ProductId ProductId { get; }

    public Quantity Quantity { get; private set; }

    public Price PriceAtAddition { get; }

    public Money LineTotal => PriceAtAddition.Multiply(Quantity.Value);

    internal void UpdateQuantity(Quantity newQuantity)
    {
        if (newQuantity.Value <= 0) throw new ArgumentException("Quantity must be positive", nameof(newQuantity));
        _units = _units.Resize(newQuantity.Value);
        Quantity = newQuantity;
    }

    internal void IncreaseQuantity() => UpdateQuantity(Quantity.Increase());

    internal void DecreaseQuantity() => UpdateQuantity(Quantity.Decrease());
}
