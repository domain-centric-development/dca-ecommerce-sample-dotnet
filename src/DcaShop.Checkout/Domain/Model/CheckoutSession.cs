using DcaShop.Checkout.Domain.Event;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>
/// A customer's checkout: buyer info → delivery → payment → review → confirmation. Step data stays null until
/// the step is fulfilled; terminal statuses prevent further changes.
/// </summary>
public sealed class CheckoutSession : AggregateRootBase<CheckoutSession, CheckoutSessionId>
{
    private readonly List<CheckoutLineItem> _lineItems;

    private CheckoutSession(CheckoutSessionId id, CartId cartId, CustomerId customerId, IReadOnlyList<CheckoutLineItem> lineItems, Money subtotal)
    {
        Id = id;
        CartId = cartId;
        CustomerId = customerId;
        _lineItems = new List<CheckoutLineItem>(lineItems);
        Totals = CheckoutTotals.Calculate(subtotal, Money.Zero(subtotal.Currency));
        CurrentStep = CheckoutStep.BuyerInfo;
        Status = CheckoutSessionStatus.Active;
    }

    public static CheckoutSession Start(CartId cartId, CustomerId customerId, IReadOnlyList<CheckoutLineItem> lineItems, Money subtotal)
    {
        if (lineItems is null || lineItems.Count == 0)
        {
            throw new EmptyCheckoutException(cartId);
        }

        var session = new CheckoutSession(CheckoutSessionId.Generate(), cartId, customerId, lineItems, subtotal);
        session.RegisterEvent(CheckoutSessionStarted.Now(session.Id, cartId, customerId, subtotal, lineItems.Count));
        return session;
    }

    public void Supersede() { EnsureModifiable(); Status = CheckoutSessionStatus.Superseded; }

    public override CheckoutSessionId Id { get; }

    public CartId CartId { get; }

    public CustomerId CustomerId { get; }

    public IReadOnlyList<CheckoutLineItem> LineItems => _lineItems.AsReadOnly();

    public CheckoutTotals Totals { get; private set; }

    public CheckoutStep CurrentStep { get; private set; }

    public CheckoutSessionStatus Status { get; private set; }

    public BuyerInfo? BuyerInfo { get; private set; }

    public DeliveryAddress? DeliveryAddress { get; private set; }

    public ShippingOption? ShippingOption { get; private set; }

    public PaymentSelection? PaymentSelection { get; private set; }

    public string? OrderReference { get; private set; }

    public bool IsActive => Status == CheckoutSessionStatus.Active;

    public bool IsCompleted => Status == CheckoutSessionStatus.Completed;

    public void SyncLineItems(IReadOnlyList<CheckoutLineItem> newLineItems, Money newSubtotal)
    {
        throw new NotSupportedException("Checkout snapshots are immutable; start a new session");
    }

    public void SubmitBuyerInfo(BuyerInfo buyerInfo)
    {
        EnsureModifiable();
        EnsureAtOrBeforeStep(CheckoutStep.BuyerInfo);
        BuyerInfo = buyerInfo ?? throw new ArgumentNullException(nameof(buyerInfo));
        if (CurrentStep == CheckoutStep.BuyerInfo)
        {
            CurrentStep = CheckoutStep.Delivery;
        }

        RegisterEvent(BuyerInfoSubmitted.Now(Id, buyerInfo));
    }

    public void SubmitDelivery(DeliveryAddress address, ShippingOption shippingOption)
    {
        EnsureModifiable();
        EnsureStepCompleted(CheckoutStep.BuyerInfo);
        EnsureAtOrBeforeStep(CheckoutStep.Delivery);
        DeliveryAddress = address ?? throw new ArgumentNullException(nameof(address));
        ShippingOption = shippingOption ?? throw new ArgumentNullException(nameof(shippingOption));
        // The tax contained in the totals moves with the shipping cost
        Totals = Totals.WithShipping(shippingOption.Cost);
        if (CurrentStep == CheckoutStep.Delivery)
        {
            CurrentStep = CheckoutStep.Payment;
        }

        RegisterEvent(DeliverySubmitted.Now(Id, address, shippingOption));
    }

    /// <summary>
    /// Asserts that this session may be paid for right now — the same preconditions
    /// <see cref="SubmitPayment"/> enforces, without changing anything.
    /// </summary>
    /// <remarks>
    /// A caller about to reach a payment provider asks this first, so a session that would be rejected afterwards
    /// never produces a payment intent at the provider.
    /// </remarks>
    public void AssertReadyForPayment()
    {
        EnsureModifiable();
        EnsureStepCompleted(CheckoutStep.BuyerInfo);
        EnsureStepCompleted(CheckoutStep.Delivery);
        EnsureAtOrBeforeStep(CheckoutStep.Payment);

        if (!Totals.Total.IsPositive)
        {
            throw new NothingToPayException(Id, Totals.Total);
        }
    }

    public void SubmitPayment(PaymentSelection payment)
    {
        EnsureModifiable();
        EnsureStepCompleted(CheckoutStep.BuyerInfo);
        EnsureStepCompleted(CheckoutStep.Delivery);
        EnsureAtOrBeforeStep(CheckoutStep.Payment);
        PaymentSelection = payment ?? throw new ArgumentNullException(nameof(payment));
        if (CurrentStep == CheckoutStep.Payment)
        {
            CurrentStep = CheckoutStep.Review;
        }

        RegisterEvent(PaymentSubmitted.Now(Id, payment));
    }

    /// <summary>
    /// Confirms the checkout once the checkout pricing has judged the current line items. The verdict and the
    /// recomputed subtotal are handed in by the use case: deciding them needs article facts the session does
    /// not own, so the domain service works them out and the session refuses to confirm an invalid one.
    /// </summary>
    public void Confirm(CheckoutValidationResult validation, Money recomputedSubtotal)
    {
        EnsureModifiable();
        EnsureAllStepsCompleted();
        if (CurrentStep != CheckoutStep.Review)
        {
            throw new CheckoutStepOutOfOrderException(Id, CheckoutStep.Review, CurrentStep);
        }

        if (!validation.IsValid)
        {
            throw new CheckoutValidationException(validation);
        }

        Totals = CheckoutTotals.Calculate(recomputedSubtotal, Totals.Shipping);
        Status = CheckoutSessionStatus.Confirmed;
        CurrentStep = CheckoutStep.Confirmation;
        RegisterEvent(CheckoutConfirmed.Now(Id, CartId, CustomerId, Totals.Total, _lineItems));
    }

    public void Complete(string? orderReference)
    {
        if (!Status.CanComplete())
        {
            throw new CheckoutNotConfirmedException(Id, Status);
        }

        OrderReference = orderReference;
        Status = CheckoutSessionStatus.Completed;
        RegisterEvent(CheckoutCompleted.Now(Id, Totals.Total, orderReference));
    }

    public void Abandon()
    {
        if (!Status.IsModifiable())
        {
            throw new CheckoutNotModifiableException(Id, Status);
        }

        var abandonedAt = CurrentStep;
        Status = CheckoutSessionStatus.Abandoned;
        RegisterEvent(CheckoutAbandoned.Now(Id, abandonedAt));
    }

    public void Expire()
    {
        if (!Status.IsModifiable())
        {
            throw new CheckoutNotModifiableException(Id, Status);
        }

        var expiredAt = CurrentStep;
        Status = CheckoutSessionStatus.Expired;
        RegisterEvent(CheckoutExpired.Now(Id, expiredAt));
    }

    public void GoBackTo(CheckoutStep step)
    {
        EnsureModifiable();
        if (step == CheckoutStep.Confirmation)
        {
            throw new CheckoutStepNotNavigableException(Id, step);
        }

        if (step.IsAfter(CurrentStep))
        {
            throw new CheckoutStepOutOfOrderException(Id, step, CurrentStep);
        }

        CurrentStep = step;
    }

    public bool IsStepCompleted(CheckoutStep step) => step switch
    {
        CheckoutStep.BuyerInfo => BuyerInfo is not null,
        CheckoutStep.Delivery => DeliveryAddress is not null && ShippingOption is not null,
        CheckoutStep.Payment => PaymentSelection is not null,
        CheckoutStep.Review => Status is CheckoutSessionStatus.Confirmed or CheckoutSessionStatus.Completed,
        CheckoutStep.Confirmation => Status == CheckoutSessionStatus.Completed,
        _ => throw new ArgumentOutOfRangeException(nameof(step)),
    };

    private void EnsureModifiable()
    {
        if (!Status.IsModifiable())
        {
            throw new CheckoutNotModifiableException(Id, Status);
        }
    }

    private void EnsureStepCompleted(CheckoutStep step)
    {
        if (!IsStepCompleted(step))
        {
            throw new CheckoutStepNotCompletedException(Id, step);
        }
    }

    private void EnsureAtOrBeforeStep(CheckoutStep step)
    {
        if (CurrentStep.IsBefore(step))
        {
            throw new CheckoutStepOutOfOrderException(Id, step, CurrentStep);
        }
    }

    private void EnsureAllStepsCompleted()
    {
        EnsureStepCompleted(CheckoutStep.BuyerInfo);
        EnsureStepCompleted(CheckoutStep.Delivery);
        EnsureStepCompleted(CheckoutStep.Payment);
    }
}
