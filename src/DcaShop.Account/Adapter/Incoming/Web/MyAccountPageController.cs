using DcaShop.Account.Application.GetAccountOverview;
using DcaShop.Account.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// Driving adapter for the account landing page. <c>[Authorize]</c> sends an anonymous visitor to the login form
/// and back here afterwards; the page itself only handles the account that no longer exists.
/// </summary>
[Route("account")]
[Authorize]
public sealed class MyAccountPageController : Controller
{
    private readonly IGetAccountOverviewInputPort _getAccountOverview;
    private readonly IIdentityService _identityService;

    public MyAccountPageController(
        IGetAccountOverviewInputPort getAccountOverview, IIdentityService identityService)
    {
        _getAccountOverview = getAccountOverview;
        _identityService = identityService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Show(CancellationToken cancellationToken)
    {
        var identity = _identityService.CurrentIdentity();
        var result = await _getAccountOverview.ExecuteAsync(
            new GetAccountOverviewQuery(identity.UserId.Value), cancellationToken);

        return result.Account is { } overview
            ? View("~/Views/Account/Overview.cshtml", MyAccountPageViewModel.From(overview))
            : Redirect(AccountRoutes.ToLoginWithReturnUrl(AccountRoutes.Account));
    }
}
