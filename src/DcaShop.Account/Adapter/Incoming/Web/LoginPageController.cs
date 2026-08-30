using System.Net;
using DcaShop.Account.Application.AuthenticateAccount;
using DcaShop.Account.Application.Shared;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// Driving adapter for the login page.
/// </summary>
/// <remarks>
/// It depends on no other context. After a successful login it always redirects to the cart's merge page with
/// the previous anonymous identity as a parameter, and the Cart context decides for itself whether anything has
/// to be merged or recovered — which is what keeps Account free of a dependency on Cart.
/// </remarks>
[Route("login")]
public sealed class LoginPageController : Controller
{
    private readonly IAuthenticateAccountInputPort _authenticateAccount;
    private readonly ITokenService _tokenService;
    private readonly IIdentityProvider _identityProvider;
    private readonly IIdentitySession _identitySession;

    public LoginPageController(
        IAuthenticateAccountInputPort authenticateAccount,
        ITokenService tokenService,
        IIdentityProvider identityProvider,
        IIdentitySession identitySession)
    {
        _authenticateAccount = authenticateAccount;
        _tokenService = tokenService;
        _identityProvider = identityProvider;
        _identitySession = identitySession;
    }

    [HttpGet("")]
    public IActionResult Show([FromQuery] string? returnUrl, [FromQuery] string? logout) =>
        View("~/Views/Account/Login.cshtml", new LoginPageViewModel(returnUrl, null, null, logout is not null));

    [HttpPost("")]
    public async Task<IActionResult> Login(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string? returnUrl,
        CancellationToken cancellationToken)
    {
        // Captured before authentication, which replaces the identity with the account's own.
        var anonymousUserId = _identityProvider.GetCurrentIdentity().UserId.Value;

        AuthenticateAccountResult result;
        try
        {
            result = await _authenticateAccount.ExecuteAsync(
                new AuthenticateAccountCommand(email, password), cancellationToken);
        }
        catch (ArgumentException e)
        {
            return Rejected(email, returnUrl, e.Message);
        }

        if (!result.Success)
        {
            return Rejected(email, returnUrl, result.ErrorMessage!);
        }

        _identitySession.SetRegisteredIdentity(
            _tokenService.GenerateRegisteredToken(UserId.Of(result.UserId!), result.Email!, result.Roles!));

        var mergeUrl = $"/cart/merge?anonymousUserId={WebUtility.UrlEncode(anonymousUserId)}";
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            mergeUrl += $"&returnUrl={WebUtility.UrlEncode(returnUrl)}";
        }

        return Redirect(mergeUrl);
    }

    private IActionResult Rejected(string email, string? returnUrl, string error) =>
        View("~/Views/Account/Login.cshtml", new LoginPageViewModel(returnUrl, email, error, false));
}
