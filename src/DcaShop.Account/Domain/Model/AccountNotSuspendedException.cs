using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Model;

/// <summary>Raised when an account that is not suspended would be reactivated.</summary>
/// <remarks>
/// Reactivation undoes a suspension. An active account has nothing to undo, and a closed one is past the point
/// where it could be undone.
/// </remarks>
public sealed class AccountNotSuspendedException : DomainException
{
    public AccountNotSuspendedException(AccountId accountId, AccountStatus status)
        : base($"Can only reactivate suspended accounts, this one is {status}")
    {
        AccountId = accountId;
        Status = status;
    }

    public AccountId AccountId { get; }

    public AccountStatus Status { get; }
}
