using DcaShop.Account.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Account.Application.ChangePassword;

/// <summary>
/// What a password change came to. The result says what happened, never in words: the page that shows it words the
/// refusal from the case. A rejected new password names the broken rule.
/// </summary>
[GenerateOneOf]
public sealed partial class ChangePasswordResult
    : OneOfBase<
        ChangePasswordResult.Changed,
        ChangePasswordResult.AccountNotAccessible,
        ChangePasswordResult.CurrentPasswordInvalid,
        ChangePasswordResult.NewPasswordRejected>
{
    /// <summary>The password was changed.</summary>
    public sealed record Changed;

    /// <summary>No account the session may act on — closed, suspended, or gone.</summary>
    public sealed record AccountNotAccessible : IRefusal;

    /// <summary>The current password does not match.</summary>
    public sealed record CurrentPasswordInvalid : IRefusal;

    /// <summary>The new password does not satisfy the password policy.</summary>
    /// <param name="BrokenRule">The part of the policy the new password breaks.</param>
    public sealed record NewPasswordRejected(PasswordRule BrokenRule) : IRefusal;
}