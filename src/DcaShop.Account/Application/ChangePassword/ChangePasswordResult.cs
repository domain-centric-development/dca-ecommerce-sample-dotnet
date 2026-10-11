using DcaShop.Account.Domain.Model;

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
/// The outcome of a password change. The result says what happened, never in words: the page that shows it words
/// the refusal from the outcome. A rejected new password names the broken rule and any other outcome does not.
/// </summary>
public sealed record ChangePasswordResult
{
    private ChangePasswordResult(ChangePasswordOutcome outcome, PasswordRule? brokenRule)
    {
        if ((outcome == ChangePasswordOutcome.NewPasswordRejected) != (brokenRule is not null))
        {
            throw new ArgumentException(
                $"A rejected new password names the broken rule, any other outcome does not: {outcome}", nameof(outcome));
        }

        Outcome = outcome;
        BrokenRule = brokenRule;
    }

    public ChangePasswordOutcome Outcome { get; }

    /// <summary>The part of the password policy the new password breaks; set exactly for a rejected one.</summary>
    public PasswordRule? BrokenRule { get; }

    public static ChangePasswordResult Changed() => new(ChangePasswordOutcome.Changed, null);

    public static ChangePasswordResult AccountNotAccessible() =>
        new(ChangePasswordOutcome.AccountNotAccessible, null);

    public static ChangePasswordResult CurrentPasswordInvalid() =>
        new(ChangePasswordOutcome.CurrentPasswordInvalid, null);

    public static ChangePasswordResult NewPasswordRejected(PasswordRule brokenRule) =>
        new(ChangePasswordOutcome.NewPasswordRejected, brokenRule);
}