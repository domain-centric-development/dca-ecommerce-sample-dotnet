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
        IIdentityProvider identityProvider)
    {
        _page = new CheckoutStepPage(this, active, session, identityProvider);
    }

    [HttpGet("review")]
    public Task<IActionResult> Review(CancellationToken cancellationToken)
    {
        var information = TempData.TakeCheckoutStepInformation(CheckoutStep.Review);
        return _page.ShowAsync(CheckoutStep.Review, (session, error) => Render(session, error, information), cancellationToken);
    }

    private Task<IActionResult> Render(CheckoutCartSnapshot session, string? error, string? information) =>
        Task.FromResult<IActionResult>(View("~/Views/Checkout/Review.cshtml", new ReviewPageViewModel(session, error, information)));
}