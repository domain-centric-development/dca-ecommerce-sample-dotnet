using DcaShop.Checkout.Application.ConfirmCheckout;
using DcaShop.Checkout.Application.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.GetConfirmedCheckoutSession;
using DcaShop.Checkout.Application.GetPaymentProviders;
using DcaShop.Checkout.Application.GetShippingOptions;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Application.StartCheckout;
using DcaShop.Checkout.Application.SubmitBuyerInfo;
using DcaShop.Checkout.Application.SubmitDelivery;
using DcaShop.Checkout.Application.SubmitPayment;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>
/// Driving adapter for the five-step checkout. Routes mirror the Java sample: the current session is resolved from
/// the customer, not from the URL (<c>/checkout/buyer</c>, <c>/checkout/delivery</c>, …).
/// </summary>
[Route("checkout")]
public sealed class CheckoutPageController : Controller
{
    private const string BuyerStep = "BuyerInfo";
    private const string DeliveryStep = "Delivery";
    private const string PaymentStep = "Payment";
    private const string ReviewStep = "Review";

    private readonly IStartCheckoutInputPort _start;
    private readonly IGetActiveCheckoutSessionInputPort _active;
    private readonly IGetConfirmedCheckoutSessionInputPort _confirmed;
    private readonly ISubmitBuyerInfoInputPort _submitBuyerInfo;
    private readonly ISubmitDeliveryInputPort _submitDelivery;
    private readonly IGetShippingOptionsInputPort _shippingOptions;
    private readonly ISubmitPaymentInputPort _submitPayment;
    private readonly IGetPaymentProvidersInputPort _paymentProviders;
    private readonly IConfirmCheckoutInputPort _confirm;

    public CheckoutPageController(
        IStartCheckoutInputPort start,
        IGetActiveCheckoutSessionInputPort active,
        IGetConfirmedCheckoutSessionInputPort confirmed,
        ISubmitBuyerInfoInputPort submitBuyerInfo,
        ISubmitDeliveryInputPort submitDelivery,
        IGetShippingOptionsInputPort shippingOptions,
        ISubmitPaymentInputPort submitPayment,
        IGetPaymentProvidersInputPort paymentProviders,
        IConfirmCheckoutInputPort confirm)
    {
        _start = start;
        _active = active;
        _confirmed = confirmed;
        _submitBuyerInfo = submitBuyerInfo;
        _submitDelivery = submitDelivery;
        _shippingOptions = shippingOptions;
        _submitPayment = submitPayment;
        _paymentProviders = paymentProviders;
        _confirm = confirm;
    }

    [HttpGet("start")]
    public async Task<IActionResult> Start([FromQuery] Guid cartId, CancellationToken cancellationToken)
    {
        try
        {
            await _start.ExecuteAsync(new StartCheckoutCommand(cartId), cancellationToken);
            return Redirect("/checkout/buyer");
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = e.Message;
            return Redirect("/cart");
        }
    }

    [HttpGet("buyer")]
    public Task<IActionResult> Buyer(CancellationToken cancellationToken) => Page(BuyerStep, cancellationToken);

    [HttpPost("buyer")]
    public Task<IActionResult> SubmitBuyer([FromForm] string email, [FromForm] string firstName, [FromForm] string lastName, [FromForm] string phone, CancellationToken cancellationToken) =>
        Submit(BuyerStep, "/checkout/delivery", id => _submitBuyerInfo.ExecuteAsync(new SubmitBuyerInfoCommand(id, email, firstName, lastName, phone), cancellationToken), cancellationToken);

    [HttpGet("delivery")]
    public Task<IActionResult> Delivery(CancellationToken cancellationToken) => Page(DeliveryStep, cancellationToken);

    [HttpPost("delivery")]
    public Task<IActionResult> SubmitDelivery([FromForm] string street, [FromForm] string? streetLine2, [FromForm] string city, [FromForm] string postalCode, [FromForm] string country, [FromForm] string? state, [FromForm] string shippingOptionId, CancellationToken cancellationToken) =>
        Submit(DeliveryStep, "/checkout/payment", id => _submitDelivery.ExecuteAsync(new SubmitDeliveryCommand(id, street, streetLine2, city, postalCode, country, state, shippingOptionId), cancellationToken), cancellationToken);

    [HttpGet("payment")]
    public Task<IActionResult> Payment(CancellationToken cancellationToken) => Page(PaymentStep, cancellationToken);

    [HttpPost("payment")]
    public Task<IActionResult> SubmitPayment([FromForm] string providerId, [FromForm] string? providerReference, CancellationToken cancellationToken) =>
        Submit(PaymentStep, "/checkout/review", id => _submitPayment.ExecuteAsync(new SubmitPaymentCommand(id, providerId, providerReference), cancellationToken), cancellationToken);

    [HttpGet("review")]
    public Task<IActionResult> Review(CancellationToken cancellationToken) => Page(ReviewStep, cancellationToken);

    [HttpPost("confirm")]
    public Task<IActionResult> Confirm(CancellationToken cancellationToken) =>
        Submit(ReviewStep, "/checkout/confirmation", id => _confirm.ExecuteAsync(new ConfirmCheckoutCommand(id), cancellationToken), cancellationToken);

    [HttpGet("confirmation")]
    public async Task<IActionResult> Confirmation(CancellationToken cancellationToken)
    {
        var customerId = GuestCustomer.Identify(HttpContext);
        var result = customerId is null ? null : await _confirmed.ExecuteAsync(new GetConfirmedCheckoutSessionQuery(customerId), cancellationToken);
        if (result?.Session is not { } session)
        {
            TempData["Error"] = "No confirmed order found";
            return Redirect("/cart");
        }

        return View("~/Views/Checkout/Confirmation.cshtml", await ViewModelAsync(session, null, cancellationToken));
    }

    private async Task<IActionResult> Page(string step, CancellationToken cancellationToken, string? error = null)
    {
        var session = await ActiveSessionAsync(cancellationToken);
        if (session is null)
        {
            TempData["Error"] = "No active checkout session found";
            return Redirect("/cart");
        }

        if (error is null && session.CurrentStep != step && StepOrder(step) > StepOrder(session.CurrentStep))
        {
            return Redirect(PathOf(session.CurrentStep));   // skipping ahead is refused
        }

        return View($"~/Views/Checkout/{step}.cshtml", await ViewModelAsync(session, error, cancellationToken));
    }

    private async Task<IActionResult> Submit(string step, string next, Func<Guid, Task> action, CancellationToken cancellationToken)
    {
        var session = await ActiveSessionAsync(cancellationToken);
        if (session is null)
        {
            TempData["Error"] = "No active checkout session found";
            return Redirect("/cart");
        }

        try
        {
            await action(session.SessionId);
            return Redirect(next);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            return await Page(step, cancellationToken, e.Message);
        }
    }

    private async Task<CheckoutSessionData?> ActiveSessionAsync(CancellationToken cancellationToken)
    {
        var customerId = GuestCustomer.Identify(HttpContext);
        if (customerId is null)
        {
            return null;
        }

        return (await _active.ExecuteAsync(new GetActiveCheckoutSessionQuery(customerId), cancellationToken)).Session;
    }

    private async Task<CheckoutPageViewModel> ViewModelAsync(CheckoutSessionData session, string? error, CancellationToken cancellationToken)
    {
        var shipping = await _shippingOptions.ExecuteAsync(new GetShippingOptionsQuery(), cancellationToken);
        var providers = await _paymentProviders.ExecuteAsync(new GetPaymentProvidersQuery(), cancellationToken);
        return new CheckoutPageViewModel(
            session,
            shipping.Options.Select(o => new CheckoutPageViewModel.ShippingChoice(o.Id, o.Name, o.EstimatedDelivery, AmountOf(o.Cost), CurrencyOf(o.Cost))).ToList(),
            providers.Providers.Select(p => new CheckoutPageViewModel.PaymentChoice(p.Id, p.DisplayName, p.Description, Available: true)).ToList(),
            error);
    }

    // "4.99 EUR" → ("4.99", "EUR"): the views print amount and currency in separate slots, like the Java views
    private static string AmountOf(string money) => money.Split(' ')[0];

    private static string CurrencyOf(string money) => money.Split(' ').ElementAtOrDefault(1) ?? string.Empty;

    private static int StepOrder(string step) => step switch
    {
        BuyerStep => 1,
        DeliveryStep => 2,
        PaymentStep => 3,
        ReviewStep => 4,
        _ => 5,
    };

    private static string PathOf(string step) => step switch
    {
        BuyerStep => "/checkout/buyer",
        DeliveryStep => "/checkout/delivery",
        PaymentStep => "/checkout/payment",
        ReviewStep => "/checkout/review",
        _ => "/checkout/confirmation",
    };
}
