using DcaShop.Checkout.Application.ConfirmCheckout;
using DcaShop.Checkout.Application.GetCheckoutSession;
using DcaShop.Checkout.Application.GetPaymentProviders;
using DcaShop.Checkout.Application.GetShippingOptions;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Application.StartCheckout;
using DcaShop.Checkout.Application.SubmitBuyerInfo;
using DcaShop.Checkout.Application.SubmitDelivery;
using DcaShop.Checkout.Application.SubmitPayment;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>Drives the five checkout steps. Each step is one page; the session decides which step is current.</summary>
[Route("checkout")]
public sealed class CheckoutPageController : Controller
{
    private readonly IStartCheckoutInputPort _start;
    private readonly IGetCheckoutSessionInputPort _get;
    private readonly ISubmitBuyerInfoInputPort _submitBuyerInfo;
    private readonly ISubmitDeliveryInputPort _submitDelivery;
    private readonly IGetShippingOptionsInputPort _shippingOptions;
    private readonly ISubmitPaymentInputPort _submitPayment;
    private readonly IGetPaymentProvidersInputPort _paymentProviders;
    private readonly IConfirmCheckoutInputPort _confirm;

    public CheckoutPageController(
        IStartCheckoutInputPort start,
        IGetCheckoutSessionInputPort get,
        ISubmitBuyerInfoInputPort submitBuyerInfo,
        ISubmitDeliveryInputPort submitDelivery,
        IGetShippingOptionsInputPort shippingOptions,
        ISubmitPaymentInputPort submitPayment,
        IGetPaymentProvidersInputPort paymentProviders,
        IConfirmCheckoutInputPort confirm)
    {
        _start = start;
        _get = get;
        _submitBuyerInfo = submitBuyerInfo;
        _submitDelivery = submitDelivery;
        _shippingOptions = shippingOptions;
        _submitPayment = submitPayment;
        _paymentProviders = paymentProviders;
        _confirm = confirm;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromForm] Guid cartId, CancellationToken cancellationToken)
    {
        var result = await _start.ExecuteAsync(new StartCheckoutCommand(cartId), cancellationToken);
        return RedirectToStep(result.Session);
    }

    [HttpGet("{sessionId:guid}/buyer-info")]
    public Task<IActionResult> BuyerInfo(Guid sessionId, CancellationToken cancellationToken) =>
        Page(sessionId, "BuyerInfo", cancellationToken);

    [HttpPost("{sessionId:guid}/buyer-info")]
    public Task<IActionResult> SubmitBuyerInfo(Guid sessionId, [FromForm] string email, [FromForm] string firstName, [FromForm] string lastName, [FromForm] string phone, CancellationToken cancellationToken) =>
        Submit(sessionId, "BuyerInfo", () => _submitBuyerInfo.ExecuteAsync(new SubmitBuyerInfoCommand(sessionId, email, firstName, lastName, phone), cancellationToken), r => r.Session, cancellationToken);

    [HttpGet("{sessionId:guid}/delivery")]
    public Task<IActionResult> Delivery(Guid sessionId, CancellationToken cancellationToken) =>
        Page(sessionId, "Delivery", cancellationToken);

    [HttpPost("{sessionId:guid}/delivery")]
    public Task<IActionResult> SubmitDelivery(Guid sessionId, [FromForm] string street, [FromForm] string? streetLine2, [FromForm] string city, [FromForm] string postalCode, [FromForm] string country, [FromForm] string? state, [FromForm] string shippingOptionId, CancellationToken cancellationToken) =>
        Submit(sessionId, "Delivery", () => _submitDelivery.ExecuteAsync(new SubmitDeliveryCommand(sessionId, street, streetLine2, city, postalCode, country, state, shippingOptionId), cancellationToken), r => r.Session, cancellationToken);

    [HttpGet("{sessionId:guid}/payment")]
    public Task<IActionResult> Payment(Guid sessionId, CancellationToken cancellationToken) =>
        Page(sessionId, "Payment", cancellationToken);

    [HttpPost("{sessionId:guid}/payment")]
    public Task<IActionResult> SubmitPayment(Guid sessionId, [FromForm] string paymentProviderId, CancellationToken cancellationToken) =>
        Submit(sessionId, "Payment", () => _submitPayment.ExecuteAsync(new SubmitPaymentCommand(sessionId, paymentProviderId), cancellationToken), r => r.Session, cancellationToken);

    [HttpGet("{sessionId:guid}/review")]
    public Task<IActionResult> Review(Guid sessionId, CancellationToken cancellationToken) =>
        Page(sessionId, "Review", cancellationToken);

    [HttpPost("{sessionId:guid}/confirm")]
    public Task<IActionResult> Confirm(Guid sessionId, CancellationToken cancellationToken) =>
        Submit(sessionId, "Review", () => _confirm.ExecuteAsync(new ConfirmCheckoutCommand(sessionId), cancellationToken), r => r.Session, cancellationToken);

    [HttpGet("{sessionId:guid}/confirmation")]
    public Task<IActionResult> Confirmation(Guid sessionId, CancellationToken cancellationToken) =>
        Page(sessionId, "Confirmation", cancellationToken);

    private async Task<IActionResult> Page(Guid sessionId, string step, CancellationToken cancellationToken, string? error = null)
    {
        var result = await _get.ExecuteAsync(new GetCheckoutSessionQuery(sessionId), cancellationToken);
        if (result.Session is not { } session)
        {
            return NotFound();
        }

        if (error is null && session.CurrentStep != step && !CanShow(session, step))
        {
            return RedirectToStep(session);
        }

        var shipping = await _shippingOptions.ExecuteAsync(new GetShippingOptionsQuery(), cancellationToken);
        var providers = await _paymentProviders.ExecuteAsync(new GetPaymentProvidersQuery(), cancellationToken);
        var vm = new CheckoutPageViewModel(
            session,
            shipping.Options.Select(o => new CheckoutPageViewModel.ShippingChoice(o.Id, o.Name, o.EstimatedDelivery, o.IsFree ? "free" : o.Cost)).ToList(),
            providers.Providers.Select(p => new CheckoutPageViewModel.PaymentChoice(p.Id, p.DisplayName, p.Description)).ToList(),
            error);
        return View($"~/Views/Checkout/{step}.cshtml", vm);
    }

    private async Task<IActionResult> Submit<TResult>(Guid sessionId, string step, Func<Task<TResult>> action, Func<TResult, CheckoutSessionData> session, CancellationToken cancellationToken)
    {
        try
        {
            return RedirectToStep(session(await action()));
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            return await Page(sessionId, step, cancellationToken, e.Message);
        }
    }

    /// <summary>Earlier steps may be revisited while the session is active; later steps only once reached.</summary>
    private static bool CanShow(CheckoutSessionData session, string step)
    {
        if (session.Status != "Active")
        {
            return step == "Confirmation";
        }

        return StepOrder(step) < StepOrder(session.CurrentStep);
    }

    private static int StepOrder(string step) => step switch
    {
        "BuyerInfo" => 1,
        "Delivery" => 2,
        "Payment" => 3,
        "Review" => 4,
        _ => 5,
    };

    private IActionResult RedirectToStep(CheckoutSessionData session) => session.CurrentStep switch
    {
        "BuyerInfo" => RedirectToAction(nameof(BuyerInfo), new { sessionId = session.SessionId }),
        "Delivery" => RedirectToAction(nameof(Delivery), new { sessionId = session.SessionId }),
        "Payment" => RedirectToAction(nameof(Payment), new { sessionId = session.SessionId }),
        "Review" => RedirectToAction(nameof(Review), new { sessionId = session.SessionId }),
        _ => RedirectToAction(nameof(Confirmation), new { sessionId = session.SessionId }),
    };
}
