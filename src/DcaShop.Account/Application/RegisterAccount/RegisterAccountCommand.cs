namespace DcaShop.Account.Application.RegisterAccount;

/// <summary>
/// Registers an account for the visitor identity the browser already carries — that is what keeps their cart.
/// </summary>
public sealed record RegisterAccountCommand(
    string Email,
    string Password,
    string CurrentUserId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth)
{
    public string Email { get; } = string.IsNullOrWhiteSpace(Email)
        ? throw new ArgumentException("Email is required", nameof(Email))
        : Email;

    public string Password { get; } = string.IsNullOrWhiteSpace(Password)
        ? throw new ArgumentException("Password is required", nameof(Password))
        : Password;

    public string CurrentUserId { get; } = string.IsNullOrWhiteSpace(CurrentUserId)
        ? throw new ArgumentException("Current user ID is required", nameof(CurrentUserId))
        : CurrentUserId;
}
