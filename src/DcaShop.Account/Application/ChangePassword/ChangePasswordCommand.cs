namespace DcaShop.Account.Application.ChangePassword;

/// <summary>A password change, authorised by the current password rather than by the session alone.</summary>
public sealed record ChangePasswordCommand(string UserId, string CurrentPassword, string NewPassword)
{
    public string UserId { get; } = string.IsNullOrWhiteSpace(UserId)
        ? throw new ArgumentException("User ID is required", nameof(UserId))
        : UserId;
}
