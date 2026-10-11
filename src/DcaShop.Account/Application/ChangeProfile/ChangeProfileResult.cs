namespace DcaShop.Account.Application.ChangeProfile;

/// <summary>Why a profile change did or did not happen.</summary>
public enum ChangeProfileOutcome
{
    Changed,

    /// <summary>No account the session may act on — closed, suspended, or gone.</summary>
    AccountNotAccessible,

    /// <summary>Another account already uses the submitted email address.</summary>
    EmailAlreadyInUse,

    /// <summary>The submitted email address is not a valid address.</summary>
    EmailInvalid,

    /// <summary>The submitted date of birth is missing or lies in the future.</summary>
    DateOfBirthInvalid,
}

/// <summary>
/// The outcome of a profile change. The result says what happened and which value was refused, never in words:
/// the page that shows it words the refusal from the outcome. Only a stored change carries the profile, and it
/// always does.
/// </summary>
public sealed record ChangeProfileResult
{
    private ChangeProfileResult(ChangeProfileOutcome outcome, ProfileView? profile)
    {
        if ((outcome == ChangeProfileOutcome.Changed) != (profile is not null))
        {
            throw new ArgumentException(
                $"Only a stored change carries the profile, and it always does: {outcome}", nameof(outcome));
        }

        Outcome = outcome;
        Profile = profile;
    }

    public ChangeProfileOutcome Outcome { get; }

    public ProfileView? Profile { get; }

    public static ChangeProfileResult Changed(ProfileView profile) =>
        new(ChangeProfileOutcome.Changed, profile);

    public static ChangeProfileResult AccountNotAccessible() =>
        new(ChangeProfileOutcome.AccountNotAccessible, null);

    public static ChangeProfileResult EmailAlreadyInUse() =>
        new(ChangeProfileOutcome.EmailAlreadyInUse, null);

    public static ChangeProfileResult EmailInvalid() =>
        new(ChangeProfileOutcome.EmailInvalid, null);

    public static ChangeProfileResult DateOfBirthInvalid() =>
        new(ChangeProfileOutcome.DateOfBirthInvalid, null);

    /// <summary>The stored values after the change.</summary>
    public sealed record ProfileView(string Email, DateOnly DateOfBirth);
}