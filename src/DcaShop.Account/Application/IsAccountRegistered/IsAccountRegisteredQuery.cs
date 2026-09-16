namespace DcaShop.Account.Application.IsAccountRegistered;

/// <summary>Asks for the existence of an account behind the identity a session names.</summary>
public sealed record IsAccountRegisteredQuery(string UserId)
{
    public string UserId { get; } = string.IsNullOrWhiteSpace(UserId)
        ? throw new ArgumentException("UserId is required", nameof(UserId))
        : UserId;
}
