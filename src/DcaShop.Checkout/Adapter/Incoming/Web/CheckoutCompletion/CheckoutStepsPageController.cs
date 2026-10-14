using DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;
using DcaShop.Checkout.Application.CheckoutCompletion.GetPaymentProviders;
using DcaShop.Checkout.Application.CheckoutCompletion.GetShippingOptions;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitDelivery;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.GetConfirmedCheckoutSession;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.SharedKernel.Application.Shared;

using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>
/// Driving adapter for the steps that complete a checkout — buyer information, delivery, payment, review and
/// confirmation. Routes mirror the Java sample: the current session is resolved from the customer, not from the URL
/// (<c>/checkout/buyer</c>, <c>/checkout/delivery</c>, …). Starting a checkout is the session feature's page.
/// </summary>
[Route("checkout")]
public sealed class CheckoutStepsPageController : Controller
{
    /// <summary>
    /// The refused "Place Order" carried to the review under a key of its own: only that refusal lets the review render
    /// without asking for step access, never an error some other page left unread.
    /// </summary>
    private const string PlaceOrderRefusal = "PlaceOrderRefusal";

    private readonly IGetActiveCheckoutSessionInputPort _active;
    private readonly IGetConfirmedCheckoutSessionInputPort _confirmed;
    private readonly ISubmitBuyerInfoInputPort _submitBuyerInfo;
    private readonly ISubmitDeliveryInputPort _submitDelivery;
    private readonly IGetShippingOptionsInputPort _shippingOptions;
    private readonly ISubmitPaymentInputPort _submitPayment;
    private readonly IGetPaymentProvidersInputPort _paymentProviders;
    private readonly IConfirmCheckoutInputPort _confirm;
    private readonly IIdentityProvider _identityProvider;

    public CheckoutStepsPageController(
        IGetActiveCheckoutSessionInputPort active,
        IGetConfirmedCheckoutSessionInputPort confirmed,
        ISubmitBuyerInfoInputPort submitBuyerInfo,
        ISubmitDeliveryInputPort submitDelivery,
        IGetShippingOptionsInputPort shippingOptions,
        ISubmitPaymentInputPort submitPayment,
        IGetPaymentProvidersInputPort paymentProviders,
        IConfirmCheckoutInputPort confirm,
        IIdentityProvider identityProvider)
    {
        _active = active;
        _confirmed = confirmed;
        _submitBuyerInfo = submitBuyerInfo;
        _submitDelivery = submitDelivery;
        _shippingOptions = shippingOptions;
        _submitPayment = submitPayment;
        _paymentProviders = paymentProviders;
        _confirm = confirm;
        _identityProvider = identityProvider;
    }

    /// <summary>
    /// The checkout session is keyed on the visitor identity, exactly as the cart is — so a guest who registers
    /// mid-checkout keeps the session they started.
    /// </summary>
    private string CurrentCustomerId() => _identityProvider.GetCurrentIdentity().UserId.Value;

    [HttpGet("buyer")]
    public Task<IActionResult> Buyer([FromQuery(Name = CheckoutRoutes.Notice)] string? notice, CancellationToken cancellationToken) =>
        Page(CheckoutStep.BuyerInfo, cancellationToken, notice: CheckoutStepNotices.FromCode(notice));

    [HttpPost("buyer")]
    public Task<IActionResult> SubmitBuyer([FromForm] string email, [FromForm] string firstName, [FromForm] string lastName, [FromForm] string phone, CancellationToken cancellationToken) =>
        Submit(CheckoutStep.BuyerInfo, "/checkout/delivery", id => _submitBuyerInfo.ExecuteAsync(new SubmitBuyerInfoCommand(id, CurrentCustomerId(), email, firstName, lastName, phone), cancellationToken), cancellationToken);

    [HttpGet("delivery")]
    public Task<IActionResult> Delivery([FromQuery(Name = CheckoutRoutes.Notice)] string? notice, CancellationToken cancellationToken) =>
        Page(CheckoutStep.Delivery, cancellationToken, notice: CheckoutStepNotices.FromCode(notice));

    [HttpPost("delivery")]
    public Task<IActionResult> SubmitDelivery([FromForm] string street, [FromForm] string? streetLine2, [FromForm] string city, [FromForm] string postalCode, [FromForm] string country, [FromForm] string? state, [FromForm] string shippingOptionId, CancellationToken cancellationToken) =>
        Submit(CheckoutStep.Delivery, "/checkout/payment", id => _submitDelivery.ExecuteAsync(new SubmitDeliveryCommand(id, CurrentCustomerId(), street, streetLine2, city, postalCode, country, state, shippingOptionId), cancellationToken), cancellationToken);

    [HttpGet("payment")]
    public Task<IActionResult> Payment([FromQuery(Name = CheckoutRoutes.Notice)] string? notice, CancellationToken cancellationToken) =>
        Page(CheckoutStep.Payment, cancellationToken, notice: CheckoutStepNotices.FromCode(notice));

    [HttpPost("payment")]
    public Task<IActionResult> SubmitPayment([FromForm] string providerId, CancellationToken cancellationToken) =>
        Submit(CheckoutStep.Payment, "/checkout/review", id => _submitPayment.ExecuteAsync(new SubmitPaymentCommand(id, CurrentCustomerId(), providerId), cancellationToken), cancellationToken);

    /// <summary>The review; a refused "Place Order" redirected here carries its error over.</summary>
    [HttpGet("review")]
    public Task<IActionResult> Review(CancellationToken cancellationToken) =>
        Page(CheckoutStep.Review, cancellationToken, TempData[PlaceOrderRefusal] as string);

    /// <summary>A refused "Place Order" redirects to the review, so the browser stays at the review's own address.</summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(CancellationToken cancellationToken)
    {
        var session = await ActiveSessionAsync(cancellationToken);
        if (session is null)
        {
            return NoActiveSession();
        }

        try
        {
            await _confirm.ExecuteAsync(new ConfirmCheckoutCommand(session.SessionId.Value, CurrentCustomerId()), cancellationToken);
            return Redirect(CheckoutRoutes.PathOf(CheckoutStep.Confirmation));
        }
        catch (Exception e) when (e is UseCaseException or DomainException)
        {
            // The confirmation carries no field the customer typed, so only the checkout's own refusals arrive here.
            return await RefusedAsync(CheckoutStep.Review, e, cancellationToken);
        }
    }

    /// <summary>The refused "Place Order" carried to the review, which renders it at its own address.</summary>
    private RedirectResult BackToTheReview(Exception refusal)
    {
        TempData[PlaceOrderRefusal] = MessageFor(CheckoutStep.Review, refusal);
        return Redirect(CheckoutRoutes.PathOf(CheckoutStep.Review));
    }

    [HttpGet("confirmation")]
    public async Task<IActionResult> Confirmation(CancellationToken cancellationToken)
    {
        var result = await _confirmed.ExecuteAsync(new GetConfirmedCheckoutSessionQuery(CurrentCustomerId()), cancellationToken);
        if (result?.Session is not { } session)
        {
            TempData["Error"] = "No confirmed order found";
            return Redirect(CheckoutRoutes.Cart);
        }

        return View("~/Views/Checkout/Confirmation.cshtml", await ViewModelAsync(session, null, cancellationToken));
    }

    /// <summary>
    /// A step's page; the <paramref name="notice"/> a redirect carried is shown only once the step guard granted the
    /// page.
    /// </summary>
    private async Task<IActionResult> Page(CheckoutStep step, CancellationToken cancellationToken, string? error = null, CheckoutStepNotice? notice = null)
    {
        // A step re-rendered with an error — a rejected submit, or a refused "Place Order" carried to the review — keeps
        // the page; otherwise the domain decides who may see it
        var result = await _active.ExecuteAsync(new GetActiveCheckoutSessionQuery(CurrentCustomerId(), error is null ? step : null), cancellationToken);
        if (result.Session is not { } session)
        {
            return NoActiveSession();
        }

        if (result.StepAccess is { Granted: false } access)
        {
            return Redirect(CheckoutRoutes.PathTo(access));
        }

        return View($"~/Views/Checkout/{step}.cshtml", await ViewModelAsync(session, error ?? notice?.Sentence(), cancellationToken));
    }

    private async Task<IActionResult> Submit(CheckoutStep step, string next, Func<Guid, Task> action, CancellationToken cancellationToken)
    {
        var session = await ActiveSessionAsync(cancellationToken);
        if (session is null)
        {
            return NoActiveSession();
        }

        try
        {
            await action(session.SessionId.Value);
            return Redirect(next);
        }
        catch (Exception e) when (e is UseCaseException or DomainException or ArgumentException)
        {
            // The two base types are the checkout's own refusals; the argument exception is the form field that
            // reaches a value object unvalidated.
            return await RefusedAsync(step, e, cancellationToken);
        }
    }

    /// <summary>
    /// The one place a step's refusal becomes an answer, whichever handler met it. A step sent before the earlier
    /// steps are done goes to the step still open, which says why; any other refusal — and an early one whose step
    /// the domain grants after all — is said by the step itself: the review at its own address, every other step
    /// re-rendered with the sentence.
    /// </summary>
    private async Task<IActionResult> RefusedAsync(CheckoutStep step, Exception refusal, CancellationToken cancellationToken)
    {
        if (refusal is CheckoutStepNotCompletedException or CheckoutStepOutOfOrderException
            && await ToTheStepStillOpenAsync(step, cancellationToken) is { } stillOpen)
        {
            return stillOpen;
        }

        return step == CheckoutStep.Review
            ? BackToTheReview(refusal)
            : await Page(step, cancellationToken, MessageFor(step, refusal));
    }

    /// <summary>
    /// Sent before the earlier steps are done: the step guard sends the customer to the step still open — none when it
    /// grants <paramref name="step"/> after all. That step says why.
    /// </summary>
    private async Task<RedirectResult?> ToTheStepStillOpenAsync(CheckoutStep step, CancellationToken cancellationToken)
    {
        var access = (await _active.ExecuteAsync(new GetActiveCheckoutSessionQuery(CurrentCustomerId(), step), cancellationToken)).StepAccess;
        return access is { Granted: false } ? Redirect(CheckoutRoutes.PathTo(access, CheckoutStepNotice.EarlierStepsFirst)) : null;
    }

    /// <summary>Back to the cart, telling the customer there is no checkout to continue.</summary>
    private RedirectResult NoActiveSession()
    {
        TempData["Error"] = "No active checkout session found";
        return Redirect(CheckoutRoutes.Cart);
    }

    /// <summary>
    /// What the customer is told when a step refuses a submission, worded from the step and the failure's type and
    /// values — the exception's message is for the log, never for the page; the provider's own wording never
    /// reaches the page either, the shop's does.
    /// </summary>
    private static string MessageFor(CheckoutStep step, Exception failure) => (step, failure) switch
    {
        (_, CheckoutSessionNotFoundException) => "Checkout session not found",
        (_, CheckoutNotModifiableException) => "This checkout can no longer be changed.",
        (CheckoutStep.Payment, PaymentInitiationFailedException) =>
            "The payment was refused. Please choose another way to pay.",
        (CheckoutStep.Payment, PaymentProviderUnavailableException) =>
            "The payment provider is not available right now. Please try again later.",
        (CheckoutStep.Payment, PaymentProviderNotFoundException) =>
            "This way to pay is not offered. Please choose another one.",
        (CheckoutStep.Payment, NothingToPayException) => "There is nothing to pay for this order.",
        (CheckoutStep.Review, CheckoutValidationException invalid) =>
            $"Cannot place the order, {invalid.Validation.Errors.Count} item(s) unavailable or out of stock.",
        (CheckoutStep.Review, CheckoutStepNotCompletedException) => CheckoutStepNotice.EarlierStepsFirst.Sentence(),
        (CheckoutStep.Review, CheckoutStepOutOfOrderException) => "Please review your order before placing it.",
        (CheckoutStep.Review, _) => "Your order could not be placed. Please try again.",
        (_, CheckoutStepNotCompletedException or CheckoutStepOutOfOrderException) => CheckoutStepNotice.EarlierStepsFirst.Sentence(),
        (CheckoutStep.BuyerInfo, ArgumentException) => "Please check the details you entered.",
        (CheckoutStep.Delivery, ArgumentException) => "Please check the delivery address you entered.",
        (CheckoutStep.Payment, ArgumentException) => "Please choose a way to pay.",
        (CheckoutStep.BuyerInfo, _) => "Your details could not be saved. Please try again.",
        (CheckoutStep.Delivery, _) => "Your delivery details could not be saved. Please try again.",
        (CheckoutStep.Payment, _) => "Your payment selection could not be saved. Please try again.",
        _ => "The checkout could not continue. Please try again.",
    };

    private async Task<CheckoutCartSnapshot?> ActiveSessionAsync(CancellationToken cancellationToken)
    {
        return (await _active.ExecuteAsync(new GetActiveCheckoutSessionQuery(CurrentCustomerId()), cancellationToken))
            .Session;
    }

    private async Task<CheckoutPageViewModel> ViewModelAsync(CheckoutCartSnapshot session, string? error, CancellationToken cancellationToken)
    {
        var shipping = await _shippingOptions.ExecuteAsync(new GetShippingOptionsQuery(), cancellationToken);
        var providers = await _paymentProviders.ExecuteAsync(new GetPaymentProvidersQuery(), cancellationToken);
        var identity = _identityProvider.GetCurrentIdentity();
        return new CheckoutPageViewModel(
            session,
            shipping.Options.Select(o => new CheckoutPageViewModel.ShippingChoice(o.Id, o.Name, o.EstimatedDelivery, AmountOf(o.Cost), CurrencyOf(o.Cost))).ToList(),
            providers.Providers.Select(p => new CheckoutPageViewModel.PaymentChoice(p.Id, p.DisplayName, Available: true)).ToList(),
            error,
            identity.IsAnonymous,
            identity.Email);
    }

    // "4.99 EUR" → ("4.99", "EUR"): the views print amount and currency in separate slots, like the Java views
    private static string AmountOf(string money) => money.Split(' ')[0];

    private static string CurrencyOf(string money) => money.Split(' ').ElementAtOrDefault(1) ?? string.Empty;
}