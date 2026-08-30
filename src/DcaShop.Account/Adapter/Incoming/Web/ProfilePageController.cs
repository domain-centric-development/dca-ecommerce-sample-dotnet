using DcaShop.Account.Application.ChangeProfile;
using DcaShop.Account.Application.GetProfile;
using DcaShop.Account.Application.Shared;
using DcaShop.SharedKernel.Application.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// Driving adapter for the profile page. The owner's name is shown but not editable — it is fixed at
/// registration.
/// </summary>
[Route("account/profile")]
public sealed class ProfilePageController : Controller
{
    internal const string ProfileUpdated = "Your profile has been updated.";

    private const string ViewName = "~/Views/Account/Profile.cshtml";

    private readonly IGetProfileInputPort _getProfile;
    private readonly IChangeProfileInputPort _changeProfile;
    private readonly IIdentityProvider _identityProvider;
    private readonly ITokenService _tokenService;
    private readonly IIdentitySession _identitySession;

    public ProfilePageController(
        IGetProfileInputPort getProfile,
        IChangeProfileInputPort changeProfile,
        IIdentityProvider identityProvider,
        ITokenService tokenService,
        IIdentitySession identitySession)
    {
        _getProfile = getProfile;
        _changeProfile = changeProfile;
        _identityProvider = identityProvider;
        _tokenService = tokenService;
        _identitySession = identitySession;
    }

    [HttpGet("")]
    public async Task<IActionResult> Show(CancellationToken cancellationToken)
    {
        var identity = _identityProvider.GetCurrentIdentity();
        if (identity.IsAnonymous)
        {
            return LoginRedirect();
        }

        var stored = await StoredProfileAsync(identity.UserId.Value, cancellationToken);
        return stored is null
            ? LoginRedirect()
            : View(ViewName, ProfilePageViewModel.Of(stored, TempData["Message"] as string));
    }

    [HttpPost("")]
    public async Task<IActionResult> Update(
        [FromForm] string email, [FromForm] string dateOfBirth, CancellationToken cancellationToken)
    {
        var identity = _identityProvider.GetCurrentIdentity();
        if (identity.IsAnonymous)
        {
            return LoginRedirect();
        }

        if (SubmittedDate.Parse(dateOfBirth) is not { } parsed)
        {
            return await RejectedAsync(identity.UserId.Value, email, dateOfBirth, SubmittedDate.NotADate, cancellationToken);
        }

        var result = await _changeProfile.ExecuteAsync(
            new ChangeProfileCommand(identity.UserId.Value, email, parsed), cancellationToken);

        switch (result.Outcome)
        {
            case ChangeProfileOutcome.Changed:
                // The email is the login credential and travels in the session token, so a changed address has
                // to be re-issued — otherwise the session would keep naming the old one.
                _identitySession.SetRegisteredIdentity(
                    _tokenService.GenerateRegisteredToken(identity.UserId, result.Profile!.Email, identity.Roles));
                TempData["Message"] = ProfileUpdated;
                return Redirect(AccountRoutes.Profile);

            case ChangeProfileOutcome.AccountNotAccessible:
                return LoginRedirect();

            default:
                return await RejectedAsync(
                    identity.UserId.Value, email, dateOfBirth, result.ErrorMessage!, cancellationToken);
        }
    }

    private async Task<IActionResult> RejectedAsync(
        string userId, string submittedEmail, string submittedDateOfBirth, string message, CancellationToken ct)
    {
        var stored = await StoredProfileAsync(userId, ct);
        return stored is null
            ? LoginRedirect()
            : View(ViewName, ProfilePageViewModel.WithError(stored, submittedEmail, submittedDateOfBirth, message));
    }

    private async Task<GetProfileResult.ProfileView?> StoredProfileAsync(string userId, CancellationToken ct) =>
        (await _getProfile.ExecuteAsync(new GetProfileQuery(userId), ct)).Profile;

    private IActionResult LoginRedirect() => Redirect(AccountRoutes.ToLoginWithReturnUrl(AccountRoutes.Profile));
}
