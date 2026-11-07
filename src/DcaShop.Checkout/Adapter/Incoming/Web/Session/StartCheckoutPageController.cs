using DcaShop.Checkout.Application.Session.StartCheckout;
using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Application.Shared;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Checkout.Adapter.Incoming.Web.Session;

/// <summary>
/// Driving adapter that starts a checkout from the cart. The cart id is posted from the cart page's checkout form,
/// so the page never reads the Cart context itself.
/// </summary>
[Route("checkout")]
public sealed class StartCheckoutPageController : Controller
{
    private readonly IStartCheckoutInputPort _start;
    private readonly IIdentityProvider _identityProvider;

    public StartCheckoutPageController(IStartCheckoutInputPort start, IIdentityProvider identityProvider)
    {
        _start = start;
        _identityProvider = identityProvider;
    }

    /// <summary>POST: starting a checkout creates a session — never reachable through a link.</summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromForm] Guid cartId, CancellationToken cancellationToken)
    {
        StartCheckoutResult result;
        try
        {
            // The checkout session is keyed on the visitor identity, exactly as the cart is.
            result = await _start.ExecuteAsync(
                new StartCheckoutCommand(cartId, _identityProvider.GetCurrentIdentity().UserId.Value), cancellationToken);
        }
        catch (Exception e) when (e is DomainException or ArgumentException)
        {
            // What is still thrown: a rule of the model no use case returns as a case, and a value a value object
            // refuses — the form field reaching it unvalidated; each field moved into request validation is one
            // reason less to catch it.
            return ToTheCart(CartNotice.CheckoutNotStarted);
        }

        return result.Match(
            started => Redirect(CheckoutRoutes.PathOf(CheckoutStep.BuyerInfo)),
            notAvailable => ToTheCart(CartNotice.CartNotFound),
            notActive => ToTheCart(CartNotice.CartNotCheckable),
            empty => ToTheCart(CartNotice.EmptyCart),
            articleNotAvailable => ToTheCart(CartNotice.CartProductUnavailable),
            unavailable => ToTheCart(CartNotice.ItemsUnavailable, unavailable.ProductIds.Count));
    }

    /// <summary>Back to the cart, leaving the cart page the notice that says why the checkout did not start.</summary>
    private RedirectResult ToTheCart(CartNotice notice, int? count = null)
    {
        TempData.LeaveForTheCart(notice, count);
        return Redirect(CheckoutRoutes.Cart);
    }
}