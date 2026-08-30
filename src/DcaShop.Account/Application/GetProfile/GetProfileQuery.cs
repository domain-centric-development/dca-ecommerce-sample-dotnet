namespace DcaShop.Account.Application.GetProfile;

/// <summary>Reads the profile of the account a cross-context identity belongs to.</summary>
public sealed record GetProfileQuery(string UserId)
{
    public string UserId { get; } = string.IsNullOrWhiteSpace(UserId)
        ? throw new ArgumentException("UserId is required", nameof(UserId))
        : UserId;
}
