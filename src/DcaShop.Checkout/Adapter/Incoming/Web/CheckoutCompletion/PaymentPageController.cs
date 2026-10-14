using DcaShop.Checkout.Application.CheckoutCompletion.GetPaymentProviders;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.SharedKernel.Application.Shared;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>Driving adapter for the payment page, where the customer chooses how to pay.</summary>
[Route("checkout")]
public sealed class PaymentPageController : Controller
{
    private readonly ISubmitPaymentInputPort _submitPayment;
    private readonly IGetPaymentProvidersInputPort _paymentProviders;
    private readonly CheckoutStepPage _page;

    public PaymentPageController(
        IGetActiveCheckoutSessionInputPort active,
        IGetCheckoutSessionInputPort session,
        ISubmitPaymentInputPort submitPayment,
        IGetPaymentProvidersInputPort paymentProviders,
        IIdentityProvider identityProvider)
    {
        _submitPayment = submitPayment;
        _paymentProviders = paymentProviders;
        _page = new CheckoutStepPage(this, active, session, identityProvider);
    }

    [HttpGet("payment")]
    public Task<IActionResult> Payment(CancellationToken cancellationToken) =>
        _page.ShowAsync(CheckoutStep.Payment, (session, error) => RenderAsync(session, error, cancellationToken), cancellationToken);

    [HttpPost("payment")]
    public Task<IActionResult> SubmitPayment([FromForm] string providerId, CancellationToken cancellationToken) =>
        _page.SubmittingAsync(
            CheckoutStep.Payment,
            CheckoutRoutes.PathOf(CheckoutStep.Review),
            id => _submitPayment.ExecuteAsync(new SubmitPaymentCommand(id, _page.CurrentCustomerId(), providerId), cancellationToken),
            (session, error) => RenderAsync(session, error, cancellationToken),
            cancellationToken);

    private async Task<IActionResult> RenderAsync(CheckoutCartSnapshot session, string? error, CancellationToken cancellationToken)
    {
        var providers = await _paymentProviders.ExecuteAsync(new GetPaymentProvidersQuery(), cancellationToken);
        return View(
            "~/Views/Checkout/Payment.cshtml",
            new PaymentPageViewModel(
                session,
                providers.Providers.Select(p => new PaymentPageViewModel.PaymentChoice(p.Id, p.DisplayName, Available: true)).ToList(),
                error));
    }
}