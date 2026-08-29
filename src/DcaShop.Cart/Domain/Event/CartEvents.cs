using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Event;

/// <summary>An item was added, or the quantity of an existing item increased by adding it again.</summary>
public sealed record CartItemAddedToCart(Guid EventId, DateTimeOffset OccurredOn, CartId CartId, ProductId ProductId, Quantity Quantity) : IDomainEvent
{
    public static CartItemAddedToCart Now(CartId cartId, ProductId productId, Quantity quantity) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, cartId, productId, quantity);
}

/// <summary>A product was removed from the cart entirely.</summary>
public sealed record ProductRemovedFromCart(Guid EventId, DateTimeOffset OccurredOn, CartId CartId, ProductId ProductId) : IDomainEvent
{
    public static ProductRemovedFromCart Now(CartId cartId, ProductId productId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, cartId, productId);
}

/// <summary>The quantity of an existing item changed.</summary>
public sealed record CartItemQuantityChanged(Guid EventId, DateTimeOffset OccurredOn, CartId CartId, CartItemId ItemId, ProductId ProductId, Quantity OldQuantity, Quantity NewQuantity) : IDomainEvent
{
    public static CartItemQuantityChanged Now(CartId cartId, CartItemId itemId, ProductId productId, Quantity oldQuantity, Quantity newQuantity) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, cartId, itemId, productId, oldQuantity, newQuantity);
}

/// <summary>All items were removed at once.</summary>
public sealed record CartCleared(Guid EventId, DateTimeOffset OccurredOn, CartId CartId, int RemovedItemCount) : IDomainEvent
{
    public static CartCleared Now(CartId cartId, int removedItemCount) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, cartId, removedItemCount);
}

/// <summary>Checkout was triggered — the cart is locked. Carries a snapshot of total and items for other contexts.</summary>
public sealed record CartCheckedOut(Guid EventId, DateTimeOffset OccurredOn, CartId CartId, CustomerId CustomerId, Money TotalAmount, IReadOnlyList<CartCheckedOut.ItemInfo> Items) : IDomainEvent
{
    public sealed record ItemInfo(ProductId ProductId, int Quantity);

    public static CartCheckedOut Now(CartId cartId, CustomerId customerId, Money totalAmount, IEnumerable<CartItem> items) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, cartId, customerId, totalAmount, items.Select(i => new ItemInfo(i.ProductId, i.Quantity.Value)).ToList());
}

/// <summary>The checkout including confirmation is finished — final state of a successful cart.</summary>
public sealed record CartCompleted(Guid EventId, DateTimeOffset OccurredOn, CartId CartId) : IDomainEvent
{
    public static CartCompleted Now(CartId cartId) => new(Guid.NewGuid(), DateTimeOffset.UtcNow, cartId);
}

/// <summary>The customer gave the cart up.</summary>
public sealed record CartAbandoned(Guid EventId, DateTimeOffset OccurredOn, CartId CartId) : IDomainEvent
{
    public static CartAbandoned Now(CartId cartId) => new(Guid.NewGuid(), DateTimeOffset.UtcNow, cartId);
}
