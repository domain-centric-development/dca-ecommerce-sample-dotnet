using DcaShop.Account.Application.GetAccountOverview;
using DcaShop.SharedKernel.Application.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>Driving adapter for the account landing page.</summary>
[Route("account")]
public sealed class MyAccountPageController : Controller
{
    private readonly IGetAccountOverviewInputPort _getAccountOverview;
    private readonly IIdentityProvider _identityProvider;

    public MyAccountPageController(
        IGetAccountOverviewInputPort getAccountOverview, IIdentityProvider identityProvider)
    {
        _getAccountOverview = getAccountOverview;
        _identityProvider = identityProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Show(CancellationToken cancellationToken)
    {
        var identity = _identityProvider.GetCurrentIdentity();
        if (identity.IsAnonymous)
        {
            return Redirect(AccountRoutes.ToLoginWithReturnUrl(AccountRoutes.Account));
        }

        var result = await _getAccountOverview.ExecuteAsync(
            new GetAccountOverviewQuery(identity.UserId.Value), cancellationToken);

        return result.Account is { } overview
            ? View("~/Views/Account/Overview.cshtml", MyAccountPageViewModel.From(overview))
            : Redirect(AccountRoutes.ToLoginWithReturnUrl(AccountRoutes.Account));
    }
}
