namespace DcaShop.Account.Application.IsAccountRegistered;

/// <summary>Asks whether an account is registered for the user id a session token names.</summary>
public sealed record IsAccountRegisteredQuery(string UserId)
{
    public string UserId { get; } = string.IsNullOrWhiteSpace(UserId)
        ? throw new ArgumentException("UserId is required", nameof(UserId))
        : UserId;
}
