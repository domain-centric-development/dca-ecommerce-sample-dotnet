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

    private CheckoutSession(CheckoutSessionId id, CartId cartId, CustomerId customerId, IReadOnlyList<CheckoutLineItem> lineItems, Money subtotal, TaxCalculator taxCalculator)
    {
        Id = id;
        CartId = cartId;
        CustomerId = customerId;
        _lineItems = new List<CheckoutLineItem>(lineItems);
        Totals = CheckoutTotals.Calculate(subtotal, Money.Zero(subtotal.Currency), taxCalculator.ContainedTax(subtotal));
        CurrentStep = CheckoutStep.BuyerInfo;
        Status = CheckoutSessionStatus.Active;
    }

    public static CheckoutSession Start(CartId cartId, CustomerId customerId, IReadOnlyList<CheckoutLineItem> lineItems, Money subtotal, TaxCalculator taxCalculator)
    {
        if (lineItems is null || lineItems.Count == 0)
        {
            throw new ArgumentException("Cannot start checkout with empty line items", nameof(lineItems));
        }

        var session = new CheckoutSession(CheckoutSessionId.Generate(), cartId, customerId, lineItems, subtotal, taxCalculator);
        session.RegisterEvent(CheckoutSessionStarted.Now(session.Id, cartId, customerId, subtotal, lineItems.Count));
        return session;
    }

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

    public void SyncLineItems(IReadOnlyList<CheckoutLineItem> newLineItems, Money newSubtotal, TaxCalculator taxCalculator)
    {
        EnsureModifiable();
        if (newLineItems is null || newLineItems.Count == 0)
        {
            throw new ArgumentException("Cannot sync with empty line items", nameof(newLineItems));
        }

        _lineItems.Clear();
        _lineItems.AddRange(newLineItems);
        Totals = CheckoutTotals.Calculate(newSubtotal, Totals.Shipping, taxCalculator.ContainedTax(newSubtotal.Add(Totals.Shipping)));
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

    public void SubmitDelivery(DeliveryAddress address, ShippingOption shippingOption, TaxCalculator taxCalculator)
    {
        EnsureModifiable();
        EnsureStepCompleted(CheckoutStep.BuyerInfo);
        EnsureAtOrBeforeStep(CheckoutStep.Delivery);
        DeliveryAddress = address ?? throw new ArgumentNullException(nameof(address));
        ShippingOption = shippingOption ?? throw new ArgumentNullException(nameof(shippingOption));
        // The tax contained in the totals moves with the shipping cost
        var withShipping = Totals.WithShipping(shippingOption.Cost);
        Totals = withShipping.WithTax(taxCalculator.ContainedTax(withShipping.Subtotal.Add(withShipping.Shipping)));
        if (CurrentStep == CheckoutStep.Delivery)
        {
            CurrentStep = CheckoutStep.Payment;
        }

        RegisterEvent(DeliverySubmitted.Now(Id, address, shippingOption));
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

    public Money CalculateOrderTotal(ICheckoutArticlePriceResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        var total = Money.Zero(Totals.Subtotal.Currency);
        foreach (var item in _lineItems)
        {
            total = total.Add(resolver.Resolve(item.ProductId).Price.Multiply(item.Quantity));
        }

        return total;
    }

    public CheckoutValidationResult ValidateItems(ICheckoutArticlePriceResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        var errors = new List<ValidationError>();
        foreach (var item in _lineItems)
        {
            var article = resolver.Resolve(item.ProductId);
            if (!article.IsAvailable)
            {
                errors.Add(ValidationError.ProductUnavailable(item.ProductId));
            }
            else if (article.AvailableStock < item.Quantity)
            {
                errors.Add(ValidationError.InsufficientStock(item.ProductId, item.Quantity, article.AvailableStock));
            }
        }

        return errors.Count == 0 ? CheckoutValidationResult.Valid() : CheckoutValidationResult.WithErrors(errors);
    }

    public void Confirm(ICheckoutArticlePriceResolver resolver)
    {
        EnsureModifiable();
        EnsureAllStepsCompleted();
        if (CurrentStep != CheckoutStep.Review)
        {
            throw new InvalidOperationException("Can only confirm from review step");
        }

        var validation = ValidateItems(resolver);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Cannot confirm checkout: validation failed with {validation.Errors.Count} error(s)");
        }

        Status = CheckoutSessionStatus.Confirmed;
        CurrentStep = CheckoutStep.Confirmation;
        RegisterEvent(CheckoutConfirmed.Now(Id, CartId, CustomerId, Totals.Total, _lineItems));
    }

    public void Complete(string? orderReference)
    {
        if (!Status.CanComplete())
        {
            throw new InvalidOperationException($"Cannot complete checkout with status: {Status}");
        }

        OrderReference = orderReference;
        Status = CheckoutSessionStatus.Completed;
        RegisterEvent(CheckoutCompleted.Now(Id, Totals.Total, orderReference));
    }

    public void Abandon()
    {
        if (Status.IsTerminal())
        {
            throw new InvalidOperationException($"Cannot abandon checkout with status: {Status}");
        }

        var abandonedAt = CurrentStep;
        Status = CheckoutSessionStatus.Abandoned;
        RegisterEvent(CheckoutAbandoned.Now(Id, abandonedAt));
    }

    public void Expire()
    {
        if (Status.IsTerminal())
        {
            throw new InvalidOperationException($"Cannot expire checkout with status: {Status}");
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
            throw new ArgumentException("Cannot navigate directly to confirmation step", nameof(step));
        }

        if (step.IsAfter(CurrentStep))
        {
            throw new ArgumentException($"Cannot skip forward to step {step} from {CurrentStep}", nameof(step));
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
            throw new InvalidOperationException($"Cannot modify checkout with status: {Status}");
        }
    }

    private void EnsureStepCompleted(CheckoutStep step)
    {
        if (!IsStepCompleted(step))
        {
            throw new InvalidOperationException($"Step {step} must be completed first");
        }
    }

    private void EnsureAtOrBeforeStep(CheckoutStep step)
    {
        if (CurrentStep.IsBefore(step))
        {
            throw new InvalidOperationException($"Cannot skip to step {step} - currently at {CurrentStep}");
        }
    }

    private void EnsureAllStepsCompleted()
    {
        if (!IsStepCompleted(CheckoutStep.BuyerInfo))
        {
            throw new InvalidOperationException("Buyer info not submitted");
        }

        if (!IsStepCompleted(CheckoutStep.Delivery))
        {
            throw new InvalidOperationException("Delivery not submitted");
        }

        if (!IsStepCompleted(CheckoutStep.Payment))
        {
            throw new InvalidOperationException("Payment not submitted");
        }
    }
}
