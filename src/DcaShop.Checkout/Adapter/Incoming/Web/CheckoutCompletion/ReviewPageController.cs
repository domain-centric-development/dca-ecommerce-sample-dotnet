using DcaShop.Checkout.Application.CheckoutCompletion.ResumeCheckout;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.SharedKernel.Application.Shared;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>
/// Driving adapter for the review page. A refused "Place Order" redirected here leaves its refusal as the review's
/// notice, and the review shows it at its own address.
/// </summary>
[Route("checkout")]
public sealed class ReviewPageController : Controller
{
    private readonly CheckoutStepPage _page;

    public ReviewPageController(
        IGetActiveCheckoutSessionInputPort active,
        IGetCheckoutSessionInputPort session,
        IResumeCheckoutInputPort resume,
        IIdentityProvider identityProvider)
    {
        _page = new CheckoutStepPage(this, active, session, resume, identityProvider);
    }

    [HttpGet("review")]
    public Task<IActionResult> Review(CancellationToken cancellationToken) =>
        _page.ShowAsync(CheckoutStep.Review, Render, cancellationToken);

    private Task<IActionResult> Render(CheckoutCartSnapshot session, string? error) =>
        Task.FromResult<IActionResult>(View("~/Views/Checkout/Review.cshtml", new ReviewPageViewModel(session, error)));
}