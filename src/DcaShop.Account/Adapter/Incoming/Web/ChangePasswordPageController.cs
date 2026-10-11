using DcaShop.Account.Application.ChangePassword;
using DcaShop.Account.Application.GetAccountOverview;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Application.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>Driving adapter for the change-password page.</summary>
[Route("account/change-password")]
[Authorize]
public sealed class ChangePasswordPageController : Controller
{
    internal const string ConfirmationMismatch = "New password and confirmation do not match";
    internal const string PasswordChanged = "Your password has been changed.";
    internal const string CurrentPasswordInvalid = "Current password is not correct";

    private const string ViewName = "~/Views/Account/ChangePassword.cshtml";

    private readonly IChangePasswordInputPort _changePassword;
    private readonly IGetAccountOverviewInputPort _getAccountOverview;
    private readonly IIdentityProvider _identityProvider;

    public ChangePasswordPageController(
        IChangePasswordInputPort changePassword,
        IGetAccountOverviewInputPort getAccountOverview,
        IIdentityProvider identityProvider)
    {
        _changePassword = changePassword;
        _getAccountOverview = getAccountOverview;
        _identityProvider = identityProvider;
    }

    [HttpGet("")]
    public async Task<IActionResult> Show(CancellationToken cancellationToken)
    {
        var identity = _identityProvider.GetCurrentIdentity();
        var accessible = (await _getAccountOverview.ExecuteAsync(
            new GetAccountOverviewQuery(identity.UserId.Value), cancellationToken)).Found;

        if (!accessible)
        {
            return LoginRedirect();
        }

        return View(ViewName, ChangePasswordPageViewModel.Blank(TempData["Message"] as string));
    }

    [HttpPost("")]
    public async Task<IActionResult> Change(
        [FromForm] string currentPassword,
        [FromForm] string newPassword,
        [FromForm] string confirmPassword,
        CancellationToken cancellationToken)
    {
        var identity = _identityProvider.GetCurrentIdentity();
        if (newPassword != confirmPassword)
        {
            return View(ViewName, ChangePasswordPageViewModel.WithError(ConfirmationMismatch));
        }

        var result = await _changePassword.ExecuteAsync(
            new ChangePasswordCommand(identity.UserId.Value, currentPassword, newPassword), cancellationToken);

        return result.Outcome switch
        {
            ChangePasswordOutcome.Changed => ChangedRedirect(),
            ChangePasswordOutcome.AccountNotAccessible => LoginRedirect(),
            ChangePasswordOutcome.CurrentPasswordInvalid =>
                View(ViewName, ChangePasswordPageViewModel.WithError(CurrentPasswordInvalid)),
            _ => View(ViewName, ChangePasswordPageViewModel.WithError(PasswordRuleSentence(result.BrokenRule!.Value))),
        };
    }

    /// <summary>
    /// The account pages' sentence for a password the policy refuses, worded from the broken rule — the rule,
    /// never the attempt. The register page uses it too.
    /// </summary>
    internal static string PasswordRuleSentence(PasswordRule rule) => rule switch
    {
        PasswordRule.TooShort => $"Password must be at least {HashedPassword.MinLength} characters long.",
        PasswordRule.TooLong => $"Password must not be longer than {HashedPassword.MaxByteLength} bytes.",
        PasswordRule.NoUppercaseLetter => "Password must contain an uppercase letter.",
        PasswordRule.NoLowercaseLetter => "Password must contain a lowercase letter.",
        PasswordRule.NoDigit => "Password must contain a digit.",
        _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, null),
    };

    private IActionResult ChangedRedirect()
    {
        TempData["Message"] = PasswordChanged;
        return Redirect(AccountRoutes.ChangePassword);
    }

    private IActionResult LoginRedirect() =>
        Redirect(AccountRoutes.ToLoginWithReturnUrl(AccountRoutes.ChangePassword));
}