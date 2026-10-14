using DcaShop.Checkout.Application.Session.StartCheckout;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Application.Shared;

using DomainCentric.BuildingBlocks.Application;
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
        try
        {
            // The checkout session is keyed on the visitor identity, exactly as the cart is.
            await _start.ExecuteAsync(
                new StartCheckoutCommand(cartId, _identityProvider.GetCurrentIdentity().UserId.Value), cancellationToken);
            return Redirect(CheckoutRoutes.PathOf(CheckoutStep.BuyerInfo));
        }
        catch (Exception e) when (e is UseCaseException or DomainException or ArgumentException)
        {
            // The two base types are the checkout's own refusals. ArgumentException is still here because a form
            // field the customer typed reaches a value object unvalidated; each field moved into request
            // validation is one reason less to catch it.
            if (e is CheckoutItemsUnavailableException unavailable)
            {
                TempData.LeaveForTheCart(CartNotice.ItemsUnavailable, unavailable.ProductIds.Count);
            }
            else
            {
                TempData.LeaveForTheCart(NoticeFor(e));
            }

            return Redirect(CheckoutRoutes.Cart);
        }
    }

    /// <summary>
    /// What the cart page tells the customer when a checkout cannot start, chosen from the failure's type — the
    /// exception's message is for the log, never for the page.
    /// </summary>
    private static CartNotice NoticeFor(Exception failure) => failure switch
    {
        CartNotAvailableException => CartNotice.CartNotFound,
        CartNotActiveException => CartNotice.CartNotCheckable,
        EmptyCartException or EmptyCheckoutException => CartNotice.EmptyCart,
        ArticleNotAvailableException => CartNotice.CartProductUnavailable,
        _ => CartNotice.CheckoutNotStarted,
    };
}