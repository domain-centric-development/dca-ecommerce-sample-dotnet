using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Account.Application.GetAccountOverview;

/// <summary>The overview, or no account the session may act on.</summary>
[GenerateOneOf]
public sealed partial class GetAccountOverviewResult
    : OneOfBase<GetAccountOverviewResult.Found, GetAccountOverviewResult.AccountNotAccessible>
{
    /// <summary>The account's overview.</summary>
    public sealed record Found(AccountOverview Account);

    /// <summary>The identity has no account the session may act on — closed, suspended, or none.</summary>
    public sealed record AccountNotAccessible : IRefusal;

    /// <summary>What the account landing page shows.</summary>
    public sealed record AccountOverview(string Email, DateTimeOffset? LastLoginAt)
    {
        public string Email { get; } = string.IsNullOrWhiteSpace(Email)
            ? throw new ArgumentException("Email is required for an account overview", nameof(Email))
            : Email;
    }
}