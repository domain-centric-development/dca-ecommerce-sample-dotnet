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
    public sealed record LineItemInfo(ProductId ProductId, int Quantity);

    public static CheckoutConfirmed Now(CheckoutSessionId sessionId, CartId cartId, CustomerId customerId, Money totalAmount, IEnumerable<CheckoutLineItem> items) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, sessionId, cartId, customerId, totalAmount, items.Select(i => new LineItemInfo(i.ProductId, i.Quantity)).ToList());
}

public sealed record CheckoutCompleted(Guid EventId, DateTimeOffset OccurredOn, CheckoutSessionId SessionId, Money TotalAmount, string? OrderReference) : IDomainEvent
{
    public static CheckoutCompleted Now(CheckoutSessionId sessionId, Money totalAmount, string? orderReference) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, sessionId, totalAmount, orderReference);
}

public sealed record CheckoutAbandoned(Guid EventId, DateTimeOffset OccurredOn, CheckoutSessionId SessionId, CheckoutStep AbandonedAt) : IDomainEvent
{
    public static CheckoutAbandoned Now(CheckoutSessionId sessionId, CheckoutStep abandonedAt) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, sessionId, abandonedAt);
}

public sealed record CheckoutExpired(Guid EventId, DateTimeOffset OccurredOn, CheckoutSessionId SessionId, CheckoutStep ExpiredAt) : IDomainEvent
{
    public static CheckoutExpired Now(CheckoutSessionId sessionId, CheckoutStep expiredAt) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, sessionId, expiredAt);
}
