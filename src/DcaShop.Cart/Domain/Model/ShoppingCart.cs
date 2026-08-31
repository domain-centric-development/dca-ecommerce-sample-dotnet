using DcaShop.Cart.Domain.Event;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>
/// A customer's shopping cart. A product appears at most once — adding it again increases the quantity.
/// Modifications are only allowed while the cart is <see cref="CartStatus.Active"/>.
/// </summary>
public sealed class ShoppingCart : AggregateRootBase<ShoppingCart, CartId>
{
    private readonly List<CartItem> _items = new();

    public ShoppingCart(CartId id, CustomerId customerId)
    {
        Id = id;
        CustomerId = customerId;
        Status = CartStatus.Active;
    }

    /// <summary>A stored line as the repository hands it back; only the aggregate turns it into a <see cref="CartItem"/>.</summary>
    public sealed record StoredItem(CartItemId Id, ProductId ProductId, Quantity Quantity, Price PriceAtAddition);

    /// <summary>Restores a stored cart as it was — no rule is re-evaluated, no event is raised.</summary>
    public static ShoppingCart Reconstitute(CartId id, CustomerId customerId, CartStatus status, IEnumerable<StoredItem> storedItems)
    {
        var cart = new ShoppingCart(id, customerId) { Status = status };
        foreach (var stored in storedItems)
        {
            cart._items.Add(new CartItem(stored.Id, stored.ProductId, stored.Quantity, stored.PriceAtAddition));
        }

        return cart;
    }

    public override CartId Id { get; }

    public CustomerId CustomerId { get; }

    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    public CartStatus Status { get; private set; }

    public bool IsActive => Status == CartStatus.Active;

    public bool IsEmpty => _items.Count == 0;

    public int ItemCount => _items.Count;

    public int TotalQuantity => _items.Sum(i => i.Quantity.Value);

    public void AddItem(ProductId productId, Quantity quantity, Price price)
    {
        EnsureCartIsActive();
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.UpdateQuantity(existing.Quantity.Add(quantity));
        }
        else
        {
            _items.Add(new CartItem(CartItemId.Generate(), productId, quantity, price));
        }

        RegisterEvent(CartItemAddedToCart.Now(Id, productId, quantity));
    }

    public void RemoveItem(CartItemId itemId)
    {
        EnsureCartIsActive();
        var item = FindItem(itemId);
        _items.Remove(item);
        RegisterEvent(ProductRemovedFromCart.Now(Id, item.ProductId));
    }

    public void RemoveItemByProductId(ProductId productId)
    {
        EnsureCartIsActive();
        if (_items.RemoveAll(i => i.ProductId == productId) == 0)
        {
            throw new ArgumentException($"Product not found in cart: {productId}", nameof(productId));
        }

        RegisterEvent(ProductRemovedFromCart.Now(Id, productId));
    }

    public void UpdateItemQuantity(CartItemId itemId, Quantity newQuantity)
    {
        EnsureCartIsActive();
        var item = FindItem(itemId);
        var oldQuantity = item.Quantity;
        item.UpdateQuantity(newQuantity);
        RegisterEvent(CartItemQuantityChanged.Now(Id, itemId, item.ProductId, oldQuantity, newQuantity));
    }

    public void IncreaseItemQuantity(CartItemId itemId)
    {
        EnsureCartIsActive();
        var item = FindItem(itemId);
        var oldQuantity = item.Quantity;
        item.IncreaseQuantity();
        RegisterEvent(CartItemQuantityChanged.Now(Id, itemId, item.ProductId, oldQuantity, item.Quantity));
    }

    public void DecreaseItemQuantity(CartItemId itemId)
    {
        EnsureCartIsActive();
        var item = FindItem(itemId);
        var oldQuantity = item.Quantity;
        item.DecreaseQuantity();
        RegisterEvent(CartItemQuantityChanged.Now(Id, itemId, item.ProductId, oldQuantity, item.Quantity));
    }

    public void Clear()
    {
        EnsureCartIsActive();
        var count = _items.Count;
        _items.Clear();
        RegisterEvent(CartCleared.Now(Id, count));
    }

    public void Checkout()
    {
        if (Status == CartStatus.CheckedOut)
        {
            throw new InvalidOperationException("Cart is already checked out");
        }

        if (IsEmpty)
        {
            throw new InvalidOperationException("Cannot checkout an empty cart");
        }

        var total = CalculateTotal();
        Status = CartStatus.CheckedOut;
        RegisterEvent(CartCheckedOut.Now(Id, CustomerId, total, _items));
    }

    public void Abandon()
    {
        Status = CartStatus.Abandoned;
        RegisterEvent(CartAbandoned.Now(Id));
    }

    public void Complete()
    {
        if (Status == CartStatus.Completed)
        {
            throw new InvalidOperationException("Cart is already completed");
        }

        if (Status == CartStatus.Abandoned)
        {
            throw new InvalidOperationException("Cannot complete an abandoned cart");
        }

        Status = CartStatus.Completed;
        RegisterEvent(CartCompleted.Now(Id));
    }

    /// <summary>Total from the prices captured at addition — the settlement price is resolved fresh at checkout.</summary>
    public Money CalculateTotal()
    {
        var total = Money.Euro(0m);
        foreach (var item in _items)
        {
            total = total.Add(item.LineTotal);
        }

        return total;
    }

    /// <summary>
    /// Total from the prices the resolver answers now, not the ones captured at addition — what the customer
    /// actually owes at settlement time.
    /// </summary>
    public Money CalculateTotal(IArticlePriceResolver priceResolver)
    {
        ArgumentNullException.ThrowIfNull(priceResolver);

        var total = Money.Euro(0m);
        foreach (var item in _items)
        {
            total = total.Add(priceResolver.Resolve(item.ProductId).Price.Multiply(item.Quantity.Value));
        }

        return total;
    }

    /// <summary>
    /// Checks every line against current availability and stock. An empty cart is valid — <see cref="Checkout"/>
    /// is what refuses it.
    /// </summary>
    public CartValidationResult ValidateForCheckout(IArticlePriceResolver priceResolver)
    {
        ArgumentNullException.ThrowIfNull(priceResolver);

        var errors = new List<CartValidationResult.ValidationError>();
        foreach (var item in _items)
        {
            var article = priceResolver.Resolve(item.ProductId);
            if (!article.IsAvailable)
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

    /// <summary>
    /// Takes over every line of another cart, keeping the price each was added at, and answers how many lines
    /// moved. The source cart is left untouched — whoever merges decides what becomes of it.
    /// </summary>
    public int Merge(ShoppingCart sourceCart)
    {
        ArgumentNullException.ThrowIfNull(sourceCart);
        EnsureCartIsActive();

        var mergedCount = 0;
        foreach (var item in sourceCart.Items)
        {
            AddItem(item.ProductId, item.Quantity, item.PriceAtAddition);
            mergedCount++;
        }

        return mergedCount;
    }

    public bool ContainsProduct(ProductId productId) => _items.Any(i => i.ProductId == productId);

    private CartItem FindItem(CartItemId itemId) =>
        _items.FirstOrDefault(i => i.Id == itemId)
        ?? throw new ArgumentException($"Cart item not found: {itemId}", nameof(itemId));

    private void EnsureCartIsActive()
    {
        if (Status != CartStatus.Active)
        {
            throw new InvalidOperationException($"Cannot modify cart with status: {Status}");
        }
    }
}
