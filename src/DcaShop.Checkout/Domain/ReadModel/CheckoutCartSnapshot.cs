using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.ReadModel;

/// <summary>
/// Query-optimised view of a <see cref="CheckoutSession"/>: every piece of state the checkout pages need,
/// immutable and detached from the aggregate. Created with <see cref="From"/>.
/// </summary>
public sealed record CheckoutCartSnapshot(
    CheckoutSessionId SessionId,
    CartId CartId,
    CustomerId CustomerId,
    CheckoutStep Step,
    CheckoutSessionStatus Status,
    IReadOnlyList<LineItemSnapshot> LineItems,
    Money Subtotal,
    CheckoutTotals Totals,
    BuyerInfo? BuyerInfo,
    DeliveryAddress? DeliveryAddress,
    ShippingOption? ShippingOption,
    PaymentSelection? PaymentSelection,
    string? OrderReference) : IValue
{
    public static CheckoutCartSnapshot From(CheckoutSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var lineItems = session.LineItems
            .Select(i => new LineItemSnapshot(i.Id, i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.ImageUrl))
            .ToList();

        return new CheckoutCartSnapshot(
            session.Id,
            session.CartId,
            session.CustomerId,
            session.CurrentStep,
            session.Status,
            lineItems,
            session.Totals.Subtotal,
            session.Totals,
            session.BuyerInfo,
            session.DeliveryAddress,
            session.ShippingOption,
            session.PaymentSelection,
            session.OrderReference);
    }

    public int ItemCount => LineItems.Count;

    public int TotalQuantity => LineItems.Sum(i => i.Quantity);

    public bool HasBuyerInfo => BuyerInfo is not null;

    public bool HasDeliveryAddress => DeliveryAddress is not null;

    public bool HasShippingOption => ShippingOption is not null;

    public bool HasPaymentSelection => PaymentSelection is not null;

    public bool HasOrderReference => OrderReference is not null;

    public bool IsActive => Status == CheckoutSessionStatus.Active;

    public bool IsConfirmed => Status == CheckoutSessionStatus.Confirmed;

    public bool IsCompleted => Status == CheckoutSessionStatus.Completed;

    /// <summary>True once the step's data is on the session — the step guard's prerequisite check.</summary>
    public bool IsStepCompleted(CheckoutStep step) => step switch
    {
        CheckoutStep.BuyerInfo => HasBuyerInfo,
        CheckoutStep.Delivery => HasDeliveryAddress && HasShippingOption,
        CheckoutStep.Payment => HasPaymentSelection,
        CheckoutStep.Review => Status is CheckoutSessionStatus.Confirmed or CheckoutSessionStatus.Completed,
        CheckoutStep.Confirmation => IsCompleted,
        _ => throw new ArgumentOutOfRangeException(nameof(step)),
    };
}
