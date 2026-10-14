using DcaShop.Account.Adapter.Incoming.Security;
using DcaShop.Account.Application.ChangeProfile;
using DcaShop.Account.Application.GetProfile;
using DcaShop.SharedKernel.Application.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// Driving adapter for the profile page. The owner's name is shown but not editable — it is fixed at
/// registration.
/// </summary>
[Route("account/profile")]
[Authorize]
public sealed class ProfilePageController : Controller
{
    internal const string EmailAlreadyInUse = "This email address is already registered";
    internal const string EmailInvalid = "Please enter a valid email address.";
    internal const string DateOfBirthInvalid = "Please enter a date of birth that is not in the future.";

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
        var notice = TempData.TakeAccountPageNotice(AccountPageNotices.ProfilePage);
        var identity = _identityProvider.GetCurrentIdentity();
        var stored = await StoredProfileAsync(identity.UserId.Value, cancellationToken);
        return stored is null
            ? LoginRedirect()
            : View(ViewName, ProfilePageViewModel.Of(stored, notice?.Sentence()));
    }

    [HttpPost("")]
    public async Task<IActionResult> Update(
        [FromForm] string email, [FromForm] string dateOfBirth, CancellationToken cancellationToken)
    {
        var identity = _identityProvider.GetCurrentIdentity();
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
                TempData.Leave(AccountPageNotices.ProfilePage, AccountPageNotice.ProfileUpdated);
                return Redirect(AccountRoutes.Profile);

            case ChangeProfileOutcome.AccountNotAccessible:
                return LoginRedirect();

            default:
                return await RejectedAsync(
                    identity.UserId.Value, email, dateOfBirth, MessageFor(result.Outcome), cancellationToken);
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

    /// <summary>The page's own sentence for a refused change, worded from the outcome.</summary>
    private static string MessageFor(ChangeProfileOutcome outcome) => outcome switch
    {
        ChangeProfileOutcome.EmailAlreadyInUse => EmailAlreadyInUse,
        ChangeProfileOutcome.EmailInvalid => EmailInvalid,
        ChangeProfileOutcome.DateOfBirthInvalid => DateOfBirthInvalid,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "not a refusal"),
    };

    private async Task<GetProfileResult.ProfileView?> StoredProfileAsync(string userId, CancellationToken ct) =>
        (await _getProfile.ExecuteAsync(new GetProfileQuery(userId), ct)).Profile;

    private IActionResult LoginRedirect() => Redirect(AccountRoutes.ToLoginWithReturnUrl(AccountRoutes.Profile));
}