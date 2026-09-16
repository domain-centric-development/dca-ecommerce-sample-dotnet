using DcaShop.Account.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>Answers the session check from the account store itself.</summary>
public sealed class AccountBasedRegisteredUserValidator : IRegisteredUserValidator
{
    private readonly IAccountRepository _accounts;

    public AccountBasedRegisteredUserValidator(IAccountRepository accounts) => _accounts = accounts;

    public async Task<bool> ExistsForUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        await _accounts.FindByLinkedUserIdAsync(userId, cancellationToken).ConfigureAwait(false) is not null;
}
