using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Model;

/// <summary>Raised when an account that may not sign in would record a login.</summary>
/// <remarks>
/// Suspended and closed accounts exist but do not let their owner in; the status says which case it is, and
/// the caller decides how much of that to tell the person at the keyboard.
/// </remarks>
public sealed class AccountNotAccessibleException : DomainException
{
    public AccountNotAccessibleException(AccountId accountId, AccountStatus status)
        : base($"Cannot login with account status: {status}")
    {
        AccountId = accountId;
        Status = status;
    }

    public AccountId AccountId { get; }

    /// <summary>The status that refuses the login.</summary>
    public AccountStatus Status { get; }
}