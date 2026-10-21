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

    public CustomerId CustomerId { get; private set; }

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
    /// Records how the customer pays and moves on to the review. The payment step only chooses the way to pay: the
    /// payment itself is authorized at "Place Order", so nothing is requested, and nothing is published, here.
    /// </summary>
    public void SubmitPayment(PaymentSelection payment)
    {
        EnsureModifiable();
        EnsureStepCompleted(CheckoutStep.BuyerInfo);
        EnsureStepCompleted(CheckoutStep.Delivery);
        EnsureAtOrBeforeStep(CheckoutStep.Payment);
        ArgumentNullException.ThrowIfNull(payment);
        if (!Totals.Total.IsPositive)
        {
            throw new NothingToPayException(Id, Totals.Total);
        }

        PaymentSelection = payment;
        if (CurrentStep == CheckoutStep.Payment)
        {
            CurrentStep = CheckoutStep.Review;
        }
    }

    /// <summary>
    /// Confirms the checkout with the payment its provider authorized at "Place Order", once the checkout pricing has
    /// judged the current line items. The verdict and the recomputed subtotal are handed in by the use case: deciding
    /// them needs article facts the session does not own. The session refuses an invalid verdict, and a payment that
    /// does not pay exactly the confirmed total through the provider the customer chose.
    /// </summary>
    public void Confirm(CheckoutValidationResult validation, Money recomputedSubtotal, PaymentSelection authorizedPayment)
    {
        AssertReadyForConfirmation(validation);
        var totals = CheckoutTotals.Calculate(recomputedSubtotal, Totals.Shipping);
        if (!authorizedPayment.Covers(PaymentSelection!.ProviderId, totals.Total))
        {
            throw new PaymentDoesNotCoverTotalException(Id, authorizedPayment.AuthorizedAmount, totals.Total);
        }

        Totals = totals;
        PaymentSelection = authorizedPayment;
        Status = CheckoutSessionStatus.Confirmed;
        CurrentStep = CheckoutStep.Confirmation;
        RegisterEvent(PaymentSubmitted.Now(Id, authorizedPayment));
        RegisterEvent(CheckoutConfirmed.Now(Id, CartId, CustomerId, Totals.Total, _lineItems));
    }

    /// <summary>
    /// Asserts that this session may be confirmed with <paramref name="validation"/> right now — the same
    /// preconditions <see cref="Confirm"/> enforces, without changing anything.
    /// </summary>
    /// <remarks>
    /// A caller about to reserve the order's stock asks this first, so a session that would be refused afterwards
    /// never holds stock back from other orders.
    /// </remarks>
    public void AssertReadyForConfirmation(CheckoutValidationResult validation)
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

    /// <summary>
    /// Hands the open checkout to the customer who owns its cart now — a guest's checkout follows the cart into the
    /// account at a login. Cart, lines and the step reached stay as they are.
    /// </summary>
    public void HandOverTo(CustomerId customerId)
    {
        EnsureModifiable();
        CustomerId = customerId;
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