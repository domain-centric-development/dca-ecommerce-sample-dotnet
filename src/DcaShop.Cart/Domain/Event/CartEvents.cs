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

/// <summary>All items were removed at once.</summary>
public sealed record CartCleared(Guid EventId, DateTimeOffset OccurredOn, CartId CartId, int RemovedItemCount) : IDomainEvent
{
    public static CartCleared Now(CartId cartId, int removedItemCount) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, cartId, removedItemCount);
}

/// <summary>The checkout including confirmation is finished — final state of a successful cart.</summary>
public sealed record CartCompleted(Guid EventId, DateTimeOffset OccurredOn, CartId CartId) : IDomainEvent
{
    public static CartCompleted Now(CartId cartId) => new(Guid.NewGuid(), DateTimeOffset.UtcNow, cartId);
}