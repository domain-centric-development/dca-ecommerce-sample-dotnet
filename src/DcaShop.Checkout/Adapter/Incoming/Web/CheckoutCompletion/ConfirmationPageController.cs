using DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Application.Session.GetConfirmedCheckoutSession;
using DcaShop.SharedKernel.Application.Shared;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>
/// Driving adapter for placing the order ("Place Order" on the review) and for the confirmation page that follows.
/// </summary>
[Route("checkout")]
public sealed class ConfirmationPageController : Controller
{
    private readonly IGetConfirmedCheckoutSessionInputPort _confirmed;
    private readonly IConfirmCheckoutInputPort _confirm;
    private readonly CheckoutStepPage _page;

    public ConfirmationPageController(
        IGetActiveCheckoutSessionInputPort active,
        IGetCheckoutSessionInputPort session,
        IGetConfirmedCheckoutSessionInputPort confirmed,
        IConfirmCheckoutInputPort confirm,
        IIdentityProvider identityProvider)
    {
        _confirmed = confirmed;
        _confirm = confirm;
        _page = new CheckoutStepPage(this, active, session, identityProvider);
    }

    /// <summary>A refused "Place Order" redirects to the review, so the browser stays at the review's own address.</summary>
    [HttpPost("confirm")]
    public Task<IActionResult> Confirm(CancellationToken cancellationToken) =>
        _page.PlacingTheOrderAsync(
            id => _confirm.ExecuteAsync(new ConfirmCheckoutCommand(id, _page.CurrentCustomerId()), cancellationToken),
            cancellationToken);

    [HttpGet("confirmation")]
    public async Task<IActionResult> Confirmation(CancellationToken cancellationToken)
    {
        var result = await _confirmed.ExecuteAsync(new GetConfirmedCheckoutSessionQuery(_page.CurrentCustomerId()), cancellationToken);
        if (result?.Session is not { } session)
        {
            return _page.ToTheCart(CartNotice.NoConfirmedOrder);
        }

        return View("~/Views/Checkout/Confirmation.cshtml", new ConfirmationPageViewModel(session));
    }
}