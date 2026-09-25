using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Model;

/// <summary>Raised when a closed account would be changed.</summary>
/// <remarks>
/// Closing is final: the account keeps its history but takes no new password, address or status. Every change
/// refuses with this one rule, whichever change it was.
/// </remarks>
public sealed class AccountClosedException : DomainException
{
    public AccountClosedException(AccountId accountId)
        : base($"Account {accountId.Value} is closed")
    {
        AccountId = accountId;
    }

    public AccountId AccountId { get; }
}