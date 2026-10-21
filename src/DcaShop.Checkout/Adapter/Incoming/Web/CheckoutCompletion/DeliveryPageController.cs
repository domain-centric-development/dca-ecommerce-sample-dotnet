using DcaShop.Checkout.Application.CheckoutCompletion.GetShippingOptions;
using DcaShop.Checkout.Application.CheckoutCompletion.ResumeCheckout;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitDelivery;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.SharedKernel.Application.Shared;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web.CheckoutCompletion;

/// <summary>Driving adapter for the delivery page, where the customer enters the address and chooses how it is shipped.</summary>
[Route("checkout")]
public sealed class DeliveryPageController : Controller
{
    private readonly ISubmitDeliveryInputPort _submitDelivery;
    private readonly IGetShippingOptionsInputPort _shippingOptions;
    private readonly CheckoutStepPage _page;

    public DeliveryPageController(
        IGetActiveCheckoutSessionInputPort active,
        IGetCheckoutSessionInputPort session,
        IResumeCheckoutInputPort resume,
        ISubmitDeliveryInputPort submitDelivery,
        IGetShippingOptionsInputPort shippingOptions,
        IIdentityProvider identityProvider)
    {
        _submitDelivery = submitDelivery;
        _shippingOptions = shippingOptions;
        _page = new CheckoutStepPage(this, active, session, resume, identityProvider);
    }

    [HttpGet("delivery")]
    public Task<IActionResult> Delivery(CancellationToken cancellationToken) =>
        _page.ShowAsync(CheckoutStep.Delivery, (session, error) => RenderAsync(session, error, cancellationToken), cancellationToken);

    [HttpPost("delivery")]
    public Task<IActionResult> SubmitDelivery([FromForm] string street, [FromForm] string? streetLine2, [FromForm] string city, [FromForm] string postalCode, [FromForm] string country, [FromForm] string? state, [FromForm] string shippingOptionId, CancellationToken cancellationToken) =>
        _page.SubmittingAsync(
            CheckoutStep.Delivery,
            CheckoutRoutes.PathOf(CheckoutStep.Payment),
            id => _submitDelivery.ExecuteAsync(new SubmitDeliveryCommand(id, _page.CurrentCustomerId(), street, streetLine2, city, postalCode, country, state, shippingOptionId), cancellationToken),
            (session, error) => RenderAsync(session, error, cancellationToken),
            cancellationToken);

    private async Task<IActionResult> RenderAsync(CheckoutCartSnapshot session, string? error, CancellationToken cancellationToken)
    {
        var shipping = await _shippingOptions.ExecuteAsync(new GetShippingOptionsQuery(), cancellationToken);
        return View(
            "~/Views/Checkout/Delivery.cshtml",
            new DeliveryPageViewModel(
                session,
                shipping.Options.Select(o => new DeliveryPageViewModel.ShippingChoice(o.Id, o.Name, o.EstimatedDelivery, AmountOf(o.Cost), CurrencyOf(o.Cost))).ToList(),
                error));
    }

    // "4.99 EUR" → ("4.99", "EUR"): the view prints amount and currency in separate slots, like the Java view
    private static string AmountOf(string money) => money.Split(' ')[0];

    private static string CurrencyOf(string money) => money.Split(' ').ElementAtOrDefault(1) ?? string.Empty;
}