using System.Net;

using DcaShop.Account.Adapter.Incoming.Security;
using DcaShop.Account.Application.AuthenticateAccount;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// Driving adapter for the login page.
/// </summary>
/// <remarks>
/// It depends on no other context. After a successful login it keeps the previous anonymous identity for the session
/// and always redirects to the cart's merge page with it as a parameter, and the Cart context decides for itself
/// whether anything has to be merged or recovered — which is what keeps Account free of a dependency on Cart.
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
        var guest = _identityProvider.GetCurrentIdentity().UserId;

        AuthenticateAccountResult result;
        try
        {
            result = await _authenticateAccount.ExecuteAsync(
                new AuthenticateAccountCommand(email, password), cancellationToken);
        }
        catch (Exception e) when (e is UseCaseException or DomainException or ArgumentException)
        {
            // The context's own refusals, plus the argument exception a form field still reaches a value object
            // with.
            return Rejected(email, returnUrl, MessageFor(e));
        }

        if (!result.Success)
        {
            return Rejected(email, returnUrl, result.ErrorMessage!);
        }

        _identitySession.SetRegisteredIdentity(
            _tokenService.GenerateRegisteredToken(UserId.Of(result.UserId!), result.Email!, result.Roles!));
        _identitySession.KeepGuestBeforeLogin(guest);

        var mergeUrl = $"/cart/merge?anonymousUserId={WebUtility.UrlEncode(guest.Value)}";
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            mergeUrl += $"&returnUrl={WebUtility.UrlEncode(returnUrl)}";
        }

        return Redirect(mergeUrl);
    }

    /// <summary>
    /// What the page tells the visitor, worded from the failure's type — the exception's message is for the log,
    /// never for the page. The account's status stays out of it: it would tell a stranger which addresses have
    /// suspended accounts.
    /// </summary>
    private static string MessageFor(Exception failure) => failure switch
    {
        AccountNotAccessibleException => "This account cannot sign in.",
        ArgumentException => "Please enter your email and your password.",
        _ => "Signing in failed. Please try again.",
    };

    private IActionResult Rejected(string email, string? returnUrl, string error) =>
        View("~/Views/Account/Login.cshtml", new LoginPageViewModel(returnUrl, email, error, false));
}