namespace DcaShop.Account.Application.GetAccountOverview;

/// <summary>The overview, or nothing when the identity has no account the session may act on.</summary>
public sealed record GetAccountOverviewResult(GetAccountOverviewResult.AccountOverview? Account)
{
    public bool Found => Account is not null;

    public static GetAccountOverviewResult NotFound() => new((AccountOverview?)null);

    /// <summary>What the account landing page shows.</summary>
    public sealed record AccountOverview(string Email, DateTimeOffset? LastLoginAt)
    {
        public string Email { get; } = string.IsNullOrWhiteSpace(Email)
            ? throw new ArgumentException("Email is required for an account overview", nameof(Email))
            : Email;
    }
}
