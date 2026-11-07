using DcaShop.Account.Adapter.Incoming.Security;
using DcaShop.Account.Application.AuthenticateAccount;
using DcaShop.Account.Application.RegisterAccount;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace DcaShop.Account.Adapter.Incoming.Api;

/// <summary>
/// Authentication for API clients. The token is returned in the body and nowhere else — this adapter sets no
/// cookie, and no cookie of the browser reaches it.
/// </summary>
/// <remarks>
/// That is the whole reason the API may skip the antiforgery token: a session established here lives in the
/// client's own storage, so a cross-site request carries no credential of its own (ADR-006, ADR-007). A browser
/// session is established by the login <i>form</i> instead, which does get cookies and does need the token.
/// </remarks>
[ApiController]
[Route("api/auth")]
public sealed class AuthResource : ControllerBase
{
    private const string AuthenticationFailedTitle = "Authentication failed";

    private readonly IAuthenticateAccountInputPort _authenticateAccount;
    private readonly IRegisterAccountInputPort _registerAccount;
    private readonly ITokenService _tokenService;
    private readonly IIdentityProvider _identityProvider;

    public AuthResource(
        IAuthenticateAccountInputPort authenticateAccount,
        IRegisterAccountInputPort registerAccount,
        ITokenService tokenService,
        IIdentityProvider identityProvider)
    {
        _authenticateAccount = authenticateAccount;
        _registerAccount = registerAccount;
        _tokenService = tokenService;
        _identityProvider = identityProvider;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await _authenticateAccount.ExecuteAsync(
            new AuthenticateAccountCommand(request.Email, request.Password), cancellationToken);

        // Every refused login answers alike, as a problem document with the API's challenge: an answer that varied
        // would tell the caller more than that the login failed.
        return result.Match(
            authenticated => Ok(LoginResponse.Succeeded(
                _tokenService.GenerateRegisteredToken(UserId.Of(authenticated.UserId), authenticated.Email, authenticated.Roles),
                authenticated.Email)),
            refused => AuthenticationFailed(),
            notAccessible => AuthenticationFailed());
    }

    [HttpPost("register")]
    [UnacceptableValueProblem]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var currentUserId = _identityProvider.GetCurrentIdentity().UserId.Value;

        // A refused registration is a case of the result; AccountProblem words its problem document.
        var result = await _registerAccount.ExecuteAsync(
            new RegisterAccountCommand(
                request.Email,
                request.Password,
                currentUserId,
                request.FirstName,
                request.LastName,
                request.DateOfBirth),
            cancellationToken);

        return result.Match<ActionResult<RegisterResponse>>(
            registered => StatusCode(
                StatusCodes.Status201Created,
                RegisterResponse.Succeeded(
                    _tokenService.GenerateRegisteredToken(UserId.Of(registered.UserId), registered.Email, registered.Roles),
                    registered.Email)),
            emailTaken => AccountProblem.EmailAlreadyRegistered(emailTaken.Email),
            alreadyExists => AccountProblem.AccountAlreadyExists(),
            weak => AccountProblem.PasswordTooWeak(weak.BrokenRule));
    }

    /// <summary>A refused login, whichever the reason.</summary>
    private ActionResult<LoginResponse> AuthenticationFailed()
    {
        Response.Headers[HeaderNames.WWWAuthenticate] = "Bearer";
        return Problem(statusCode: StatusCodes.Status401Unauthorized, title: AuthenticationFailedTitle);
    }

    /// <summary>
    /// Stateless: there is no session here to end. The endpoint exists so a client has one uniform place to call
    /// when it discards its token.
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();
}