namespace DcaShop.Account.Application.GetAccountOverview;

/// <summary>Reads the account landing page of a cross-context identity.</summary>
public sealed record GetAccountOverviewQuery(string UserId)
{
    public string UserId { get; } = string.IsNullOrWhiteSpace(UserId)
        ? throw new ArgumentException("UserId is required", nameof(UserId))
        : UserId;
}
