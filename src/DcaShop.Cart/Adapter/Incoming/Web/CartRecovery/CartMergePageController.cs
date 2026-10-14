using System.Net;

using DcaShop.Cart.Application.CartRecovery.GetCartMergeOptions;
using DcaShop.Cart.Application.CartRecovery.MergeCarts;
using DcaShop.Cart.Application.CartRecovery.RecoverCart;
using DcaShop.SharedKernel.Application.Shared;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Cart.Adapter.Incoming.Web.CartRecovery;

/// <summary>
/// Driving adapter for the cart merge page — where a visitor who just logged in decides what happens to the cart
/// they filled as a guest.
/// </summary>
/// <remarks>
/// <para>
/// The Account context redirects here after every login and hands over the identity the browser had before,
/// because it cannot know whether there is anything to reconcile. This controller answers that question, which
/// is what keeps Account free of a dependency on Cart.
/// </para>
/// <para>
/// State travels in the URL rather than in a session, so the page works on a stateless chain.
/// </para>
/// </remarks>
[Route("cart/merge")]
public sealed class CartMergePageController : Controller
{
    private const string CartPage = "/cart";

    private readonly IGetCartMergeOptionsInputPort _getMergeOptions;
    private readonly IMergeCartsInputPort _mergeCarts;
    private readonly IRecoverCartInputPort _recoverCart;
    private readonly IIdentityProvider _identityProvider;

    public CartMergePageController(
        IGetCartMergeOptionsInputPort getMergeOptions,
        IMergeCartsInputPort mergeCarts,
        IRecoverCartInputPort recoverCart,
        IIdentityProvider identityProvider)
    {
        _getMergeOptions = getMergeOptions;
        _mergeCarts = mergeCarts;
        _recoverCart = recoverCart;
        _identityProvider = identityProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Show(
        [FromQuery] string anonymousUserId,
        [FromQuery] string? returnUrl,
        CancellationToken cancellationToken)
    {
        var (notice, _) = TempData.TakeCartPageNotice(CartPageNotices.MergePage);
        var registeredUserId = _identityProvider.GetCurrentIdentity().UserId.Value;
        var options = await _getMergeOptions.ExecuteAsync(
            new GetCartMergeOptionsQuery(anonymousUserId, registeredUserId), cancellationToken);

        if (!options.MergeRequired)
        {
            // Nothing to decide does not mean nothing to do: when only the guest cart holds items, they still
            // have to follow the visitor into their account, or logging in would silently empty the cart.
            await _recoverCart.ExecuteAsync(
                new RecoverCartCommand(anonymousUserId, registeredUserId), cancellationToken);

            return RedirectWithNoticeForTheCart(Destination(returnUrl), CartPageNotice.WelcomeBack);
        }

        return View(
            "~/Views/Cart/MergeOptions.cshtml",
            CartMergePageViewModel.FromResult(options, anonymousUserId, returnUrl) with
            {
                // The one notice this page says; any other code is not its own and shows nothing.
                ErrorMessage = notice is CartPageNotice.InvalidMergeOption
                    ? CartPageNotice.InvalidMergeOption.Sentence()
                    : null,
            });
    }

    [HttpPost("")]
    public async Task<IActionResult> Decide(
        [FromForm] string strategy,
        [FromForm] string anonymousUserId,
        [FromForm] string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (CartMergeStrategySubmission.Parse(strategy) is not { } chosen)
        {
            var back = $"/cart/merge?anonymousUserId={WebUtility.UrlEncode(anonymousUserId)}";
            TempData.Leave(CartPageNotices.MergePage, CartPageNotice.InvalidMergeOption);
            return Redirect(
                string.IsNullOrWhiteSpace(returnUrl) ? back : $"{back}&returnUrl={WebUtility.UrlEncode(returnUrl)}");
        }

        await _mergeCarts.ExecuteAsync(
            new MergeCartsCommand(anonymousUserId, _identityProvider.GetCurrentIdentity().UserId.Value, chosen),
            cancellationToken);

        return RedirectWithNoticeForTheCart(Destination(returnUrl), chosen switch
        {
            CartMergeStrategy.MergeBoth => CartPageNotice.CartsMerged,
            CartMergeStrategy.UseAccountCart => CartPageNotice.AccountCartUsed,
            _ => CartPageNotice.RecentCartUsed,
        });
    }

    private static string Destination(string? returnUrl) =>
        string.IsNullOrWhiteSpace(returnUrl) ? CartPage : returnUrl;

    /// <summary>
    /// To the destination, leaving the notice when it is the cart page — the one page that says it; any other page the
    /// visitor was headed to opens without one, and no notice waits for a later visit to the cart.
    /// </summary>
    private RedirectResult RedirectWithNoticeForTheCart(string destination, CartPageNotice notice)
    {
        if (destination == CartPage)
        {
            TempData.Leave(CartPageNotices.CartPage, notice);
        }

        return Redirect(destination);
    }
}