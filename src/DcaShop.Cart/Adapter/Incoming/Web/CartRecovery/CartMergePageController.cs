using System.Net;
using DcaShop.Cart.Application.GetCartMergeOptions;
using DcaShop.Cart.Application.MergeCarts;
using DcaShop.Cart.Application.RecoverCartOnLogin;
using DcaShop.SharedKernel.Application.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Cart.Adapter.Incoming.Web;

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
    private readonly IGetCartMergeOptionsInputPort _getMergeOptions;
    private readonly IMergeCartsInputPort _mergeCarts;
    private readonly IRecoverCartOnLoginInputPort _recoverCart;
    private readonly IIdentityProvider _identityProvider;

    public CartMergePageController(
        IGetCartMergeOptionsInputPort getMergeOptions,
        IMergeCartsInputPort mergeCarts,
        IRecoverCartOnLoginInputPort recoverCart,
        IIdentityProvider identityProvider)
    {
        _getMergeOptions = getMergeOptions;
        _mergeCarts = mergeCarts;
        _recoverCart = recoverCart;
        _identityProvider = identityProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Show(
        [FromQuery] string anonymousUserId, [FromQuery] string? returnUrl, CancellationToken cancellationToken)
    {
        var registeredUserId = _identityProvider.GetCurrentIdentity().UserId.Value;
        var options = await _getMergeOptions.ExecuteAsync(
            new GetCartMergeOptionsQuery(anonymousUserId, registeredUserId), cancellationToken);

        if (!options.MergeRequired)
        {
            // Nothing to decide does not mean nothing to do: when only the guest cart holds items, they still
            // have to follow the visitor into their account, or logging in would silently empty the cart.
            await _recoverCart.ExecuteAsync(
                new RecoverCartOnLoginCommand(anonymousUserId, registeredUserId), cancellationToken);

            TempData["Message"] = "Welcome back!";
            return Redirect(Destination(returnUrl));
        }

        return View(
            "~/Views/Cart/MergeOptions.cshtml",
            CartMergePageViewModel.FromResult(options, anonymousUserId, returnUrl));
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
            TempData["Error"] = "Invalid merge option selected";
            var back = $"/cart/merge?anonymousUserId={WebUtility.UrlEncode(anonymousUserId)}";
            return Redirect(string.IsNullOrWhiteSpace(returnUrl)
                ? back
                : $"{back}&returnUrl={WebUtility.UrlEncode(returnUrl)}");
        }

        await _mergeCarts.ExecuteAsync(
            new MergeCartsCommand(anonymousUserId, _identityProvider.GetCurrentIdentity().UserId.Value, chosen),
            cancellationToken);

        TempData["Message"] = chosen switch
        {
            CartMergeStrategy.MergeBoth => "Carts merged successfully!",
            CartMergeStrategy.UseAccountCart => "Using your account cart.",
            _ => "Using your recent cart.",
        };

        return Redirect(Destination(returnUrl));
    }

    private static string Destination(string? returnUrl) =>
        string.IsNullOrWhiteSpace(returnUrl) ? "/cart" : returnUrl;
}
