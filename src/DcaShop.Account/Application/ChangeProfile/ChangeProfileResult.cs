namespace DcaShop.Account.Application.ChangeProfile;

/// <summary>Why a profile change did or did not happen.</summary>
public enum ChangeProfileOutcome
{
    Changed,

    /// <summary>No account the session may act on — closed, suspended, or gone.</summary>
    AccountNotAccessible,

    EmailAlreadyInUse,

    /// <summary>A submitted value broke a domain rule.</summary>
    InputRejected,
}

/// <summary>
/// The outcome of a profile change. A rejecting outcome names its reason and any other outcome does not; only a
/// stored change carries the profile, and it always does.
/// </summary>
public sealed record ChangeProfileResult
{
    private ChangeProfileResult(ChangeProfileOutcome outcome, string? errorMessage, ProfileView? profile)
    {
        var rejecting = outcome is ChangeProfileOutcome.EmailAlreadyInUse or ChangeProfileOutcome.InputRejected;
        if (rejecting != (errorMessage is not null))
        {
            throw new ArgumentException(
                $"A rejecting outcome must name its reason, any other outcome must not: {outcome}", nameof(outcome));
        }

        if ((outcome == ChangeProfileOutcome.Changed) != (profile is not null))
        {
            throw new ArgumentException(
                $"Only a stored change carries the profile, and it always does: {outcome}", nameof(outcome));
        }

        Outcome = outcome;
        ErrorMessage = errorMessage;
        Profile = profile;
    }

    public ChangeProfileOutcome Outcome { get; }

    public string? ErrorMessage { get; }

    public ProfileView? Profile { get; }

    public static ChangeProfileResult Changed(ProfileView profile) =>
        new(ChangeProfileOutcome.Changed, null, profile);

    public static ChangeProfileResult AccountNotAccessible() =>
        new(ChangeProfileOutcome.AccountNotAccessible, null, null);

    public static ChangeProfileResult EmailAlreadyInUse(string message) =>
        new(ChangeProfileOutcome.EmailAlreadyInUse, message, null);

    public static ChangeProfileResult InputRejected(string message) =>
        new(ChangeProfileOutcome.InputRejected, message, null);

    /// <summary>The stored values after the change.</summary>
    public sealed record ProfileView(string Email, DateOnly DateOfBirth);
}
