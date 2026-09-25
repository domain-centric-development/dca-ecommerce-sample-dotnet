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


    /// <summary>
    /// Whether this snapshot may be shown at the requested step, and where to send the customer instead.
    /// Business logic, not presentation: it holds whether the interface shows one page or five. It decides on
    /// the snapshot alone, so a read does not have to load the session aggregate.
    /// </summary>
    public StepAccess AccessTo(CheckoutStep targetStep)
    {
        if (Status.IsTerminal())
        {
            return TerminalStateAccess(targetStep);
        }

        if (Status.CanComplete())
        {
            return targetStep == CheckoutStep.Confirmation ? StepAccess.Grant() : StepAccess.RedirectTo(CheckoutStep.Confirmation);
        }

        if (targetStep == CheckoutStep.Confirmation)
        {
            return IsCompleted ? StepAccess.Grant() : StepAccess.RedirectTo(Step);
        }

        return IsSkippingAhead(targetStep) ? StepAccess.RedirectTo(Step) : StepAccess.Grant();
    }

    private StepAccess TerminalStateAccess(CheckoutStep targetStep) => Status switch
    {
        CheckoutSessionStatus.Completed => targetStep == CheckoutStep.Confirmation ? StepAccess.Grant() : StepAccess.RedirectTo(CheckoutStep.Confirmation),
        CheckoutSessionStatus.Superseded or CheckoutSessionStatus.Abandoned or CheckoutSessionStatus.Expired => StepAccess.BackToCart(),
        _ => StepAccess.Grant(),
    };

    private bool IsSkippingAhead(CheckoutStep targetStep) =>
        targetStep.IsAfter(Step) || !ArePrerequisitesMet(targetStep);

    private bool ArePrerequisitesMet(CheckoutStep targetStep) => targetStep switch
    {
        CheckoutStep.BuyerInfo => true,
        CheckoutStep.Delivery => IsStepCompleted(CheckoutStep.BuyerInfo),
        CheckoutStep.Payment => IsStepCompleted(CheckoutStep.BuyerInfo) && IsStepCompleted(CheckoutStep.Delivery),
        CheckoutStep.Review => IsStepCompleted(CheckoutStep.BuyerInfo) && IsStepCompleted(CheckoutStep.Delivery) && IsStepCompleted(CheckoutStep.Payment),
        CheckoutStep.Confirmation => IsCompleted,
        _ => throw new ArgumentOutOfRangeException(nameof(targetStep)),
    };
}