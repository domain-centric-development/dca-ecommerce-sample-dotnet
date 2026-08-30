namespace DcaShop.Account.Application.ChangePassword;

/// <summary>Why a password change did or did not happen.</summary>
public enum ChangePasswordOutcome
{
    Changed,

    /// <summary>No account the session may act on — closed, suspended, or gone.</summary>
    AccountNotAccessible,

    CurrentPasswordInvalid,

    /// <summary>The new password does not satisfy the password policy.</summary>
    NewPasswordRejected,
}

/// <summary>
/// The outcome of a password change. A rejecting outcome names its reason and any other outcome does not — the
/// controller renders that message to the user verbatim, so it must always be one meant for them.
/// </summary>
public sealed record ChangePasswordResult
{
    private ChangePasswordResult(ChangePasswordOutcome outcome, string? errorMessage)
    {
        var rejecting = outcome is ChangePasswordOutcome.CurrentPasswordInvalid
            or ChangePasswordOutcome.NewPasswordRejected;
        if (rejecting != (errorMessage is not null))
        {
            throw new ArgumentException(
                $"A rejecting outcome must name its reason, any other outcome must not: {outcome}", nameof(outcome));
        }

        Outcome = outcome;
        ErrorMessage = errorMessage;
    }

    public ChangePasswordOutcome Outcome { get; }

    public string? ErrorMessage { get; }

    public static ChangePasswordResult Changed() => new(ChangePasswordOutcome.Changed, null);

    public static ChangePasswordResult AccountNotAccessible() =>
        new(ChangePasswordOutcome.AccountNotAccessible, null);

    public static ChangePasswordResult CurrentPasswordInvalid(string message) =>
        new(ChangePasswordOutcome.CurrentPasswordInvalid, message);

    public static ChangePasswordResult NewPasswordRejected(string message) =>
        new(ChangePasswordOutcome.NewPasswordRejected, message);
}
