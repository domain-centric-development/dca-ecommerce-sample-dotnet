using DcaShop.Account.Application.AuthenticateAccount;
using DcaShop.Account.Application.RegisterAccount;
using DcaShop.Account.Application.Shared;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, LoginResponse.Failed(result.ErrorMessage));
        }

        var token = _tokenService.GenerateRegisteredToken(
            UserId.Of(result.UserId!), result.Email!, result.Roles!);
        return Ok(LoginResponse.Succeeded(token, result.Email!));
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var currentUserId = _identityProvider.GetCurrentIdentity().UserId.Value;

        try
        {
            var result = await _registerAccount.ExecuteAsync(
                new RegisterAccountCommand(
                    request.Email,
                    request.Password,
                    currentUserId,
                    request.FirstName,
                    request.LastName,
                    request.DateOfBirth),
                cancellationToken);

            var token = _tokenService.GenerateRegisteredToken(
                UserId.Of(result.UserId), result.Email, result.Roles);
            return StatusCode(StatusCodes.Status201Created, RegisterResponse.Succeeded(token, result.Email));
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            return BadRequest(RegisterResponse.Failed(e.Message));
        }
    }

    /// <summary>
    /// Stateless: there is no session here to end. The endpoint exists so a client has one uniform place to call
    /// when it discards its token.
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();
}
