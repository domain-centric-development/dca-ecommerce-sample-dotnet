using DcaShop.Checkout.Application.CheckoutCompletion.ResumeCheckout;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.SharedKernel.Application.Shared;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>Driving adapter for the buyer information page, where the customer enters their contact details.</summary>
[Route("checkout")]
public sealed class BuyerInfoPageController : Controller
{
    private readonly ISubmitBuyerInfoInputPort _submitBuyerInfo;
    private readonly IIdentityProvider _identityProvider;
    private readonly CheckoutStepPage _page;

    public BuyerInfoPageController(
        IGetActiveCheckoutSessionInputPort active,
        IGetCheckoutSessionInputPort session,
        IResumeCheckoutInputPort resume,
        ISubmitBuyerInfoInputPort submitBuyerInfo,
        IIdentityProvider identityProvider)
    {
        _submitBuyerInfo = submitBuyerInfo;
        _identityProvider = identityProvider;
        _page = new CheckoutStepPage(this, active, session, resume, identityProvider);
    }

    [HttpGet("buyer")]
    public Task<IActionResult> Buyer(CancellationToken cancellationToken) =>
        _page.ShowAsync(CheckoutStep.BuyerInfo, Render, cancellationToken);

    [HttpPost("buyer")]
    public Task<IActionResult> SubmitBuyer([FromForm] string email, [FromForm] string firstName, [FromForm] string lastName, [FromForm] string phone, CancellationToken cancellationToken) =>
        _page.SubmittingAsync(
            CheckoutStep.BuyerInfo,
            CheckoutRoutes.PathOf(CheckoutStep.Delivery),
            id => _submitBuyerInfo.ExecuteAsync(new SubmitBuyerInfoCommand(id, _page.CurrentCustomerId(), email, firstName, lastName, phone), cancellationToken),
            Render,
            cancellationToken);

    private Task<IActionResult> Render(CheckoutCartSnapshot session, string? error)
    {
        var identity = _identityProvider.GetCurrentIdentity();
        return Task.FromResult<IActionResult>(View(
            "~/Views/Checkout/BuyerInfo.cshtml",
            new BuyerInfoPageViewModel(session, error, identity.IsAnonymous, identity.Email)));
    }
}