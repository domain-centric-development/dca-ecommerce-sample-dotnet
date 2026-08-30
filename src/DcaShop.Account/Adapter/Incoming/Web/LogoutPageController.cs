using DcaShop.Account.Application.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// Ends the session. It is a <c>POST</c> because it changes state, and it carries an antiforgery token like
/// every other writing form (ADR-005).
/// </summary>
[Route("logout")]
public sealed class LogoutPageController : Controller
{
    private readonly IIdentitySession _identitySession;

    public LogoutPageController(IIdentitySession identitySession) => _identitySession = identitySession;

    [HttpPost("")]
    public IActionResult Logout()
    {
        _identitySession.LogOut();
        return Redirect($"{AccountRoutes.Login}?logout=true");
    }
}
