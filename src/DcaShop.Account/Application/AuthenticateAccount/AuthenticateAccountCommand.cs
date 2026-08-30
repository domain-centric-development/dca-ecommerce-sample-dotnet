namespace DcaShop.Account.Application.AuthenticateAccount;

/// <summary>Credentials submitted by a login form.</summary>
public sealed record AuthenticateAccountCommand(string Email, string Password)
{
    public string Email { get; } = string.IsNullOrWhiteSpace(Email)
        ? throw new ArgumentException("Email is required", nameof(Email))
        : Email;

    public string Password { get; } = string.IsNullOrWhiteSpace(Password)
        ? throw new ArgumentException("Password is required", nameof(Password))
        : Password;
}
