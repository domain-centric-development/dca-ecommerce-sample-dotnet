using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Event;

public sealed record CheckoutSessionStarted(Guid EventId, DateTimeOffset OccurredOn, CheckoutSessionId SessionId, CartId CartId, CustomerId CustomerId, Money Subtotal, int ItemCount) : IDomainEvent
{
    public static CheckoutSessionStarted Now(CheckoutSessionId sessionId, CartId cartId, CustomerId customerId, Money subtotal, int itemCount) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, sessionId, cartId, customerId, subtotal, itemCount);
}

public sealed record BuyerInfoSubmitted(Guid EventId, DateTimeOffset OccurredOn, CheckoutSessionId SessionId, BuyerInfo BuyerInfo) : IDomainEvent
{
    public static BuyerInfoSubmitted Now(CheckoutSessionId sessionId, BuyerInfo buyerInfo) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, sessionId, buyerInfo);
}

public sealed record DeliverySubmitted(Guid EventId, DateTimeOffset OccurredOn, CheckoutSessionId SessionId, DeliveryAddress Address, ShippingOption ShippingOption) : IDomainEvent
{
    public static DeliverySubmitted Now(CheckoutSessionId sessionId, DeliveryAddress address, ShippingOption shippingOption) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, sessionId, address, shippingOption);
}

public sealed record PaymentSubmitted(Guid EventId, DateTimeOffset OccurredOn, CheckoutSessionId SessionId, PaymentSelection Payment) : IDomainEvent
{
    public static PaymentSubmitted Now(CheckoutSessionId sessionId, PaymentSelection payment) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, sessionId, payment);
}

/// <summary>The customer confirmed the order at the review step. Relayed to other contexts as <c>CheckoutConfirmedEvent</c>.</summary>
public sealed record CheckoutConfirmed(Guid EventId, DateTimeOffset OccurredOn, CheckoutSessionId SessionId, CartId CartId, CustomerId CustomerId, Money TotalAmount, IReadOnlyList<CheckoutConfirmed.LineItemInfo> Items) : IDomainEvent
{
    public sealed record LineItemInfo(ProductId ProductId, int Quantity, string PositionSnapshot = "");

    public static CheckoutConfirmed Now(CheckoutSessionId sessionId, CartId cartId, CustomerId customerId, Money totalAmount, IEnumerable<CheckoutLineItem> items) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, sessionId, cartId, customerId, totalAmount, items.Select(i => new LineItemInfo(i.ProductId, i.Quantity, i.PositionSnapshot)).ToList());
}