using DcaShop.Checkout.Application.CheckoutCompletion.GetReview;
using DcaShop.Checkout.Application.CheckoutCompletion.ResumeCheckout;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.SharedKernel.Application.Shared;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>
/// Driving adapter for the review page. The review shows the order at today's prices, and its "Place Order" carries the
/// fingerprint of what it showed. A refused "Place Order" redirected here leaves its refusal as the review's notice,
/// and the review shows it at its own address.
/// </summary>
[Route("checkout")]
public sealed class ReviewPageController : Controller
{
    private readonly CheckoutStepPage _page;
    private readonly IGetReviewInputPort _review;

    public ReviewPageController(
        IGetActiveCheckoutSessionInputPort active,
        IGetCheckoutSessionInputPort session,
        IResumeCheckoutInputPort resume,
        IGetReviewInputPort review,
        IIdentityProvider identityProvider)
    {
        _page = new CheckoutStepPage(this, active, session, resume, identityProvider);
        _review = review;
    }

    [HttpGet("review")]
    public Task<IActionResult> Review(CancellationToken cancellationToken) =>
        _page.ShowAsync(CheckoutStep.Review, (session, error) => RenderAsync(session, error, cancellationToken), cancellationToken);

    private async Task<IActionResult> RenderAsync(CheckoutCartSnapshot session, string? error, CancellationToken cancellationToken)
    {
        var review = await _review.ExecuteAsync(new GetReviewQuery(session.SessionId.Value, _page.CurrentCustomerId()), cancellationToken);
        return View("~/Views/Checkout/Review.cshtml", new ReviewPageViewModel(review.Order, review.Fingerprint, error));
    }
}