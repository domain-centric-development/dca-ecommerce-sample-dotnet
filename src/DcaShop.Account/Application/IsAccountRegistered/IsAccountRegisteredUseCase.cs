using DcaShop.Account.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Application.IsAccountRegistered;

/// <summary>
/// Looks the account up by its linked user ID. A token that names an account nobody can find is stale — whether
/// the account was deleted or the store never kept it.
/// </summary>
public sealed class IsAccountRegisteredUseCase : IIsAccountRegisteredInputPort
{
    private readonly IAccountRepository _accounts;

    public IsAccountRegisteredUseCase(IAccountRepository accounts) => _accounts = accounts;

    public async Task<IsAccountRegisteredResult> ExecuteAsync(
        IsAccountRegisteredQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var account = await _accounts
            .FindByLinkedUserIdAsync(UserId.Of(query.UserId), cancellationToken)
            .ConfigureAwait(false);
        return new IsAccountRegisteredResult(account is not null);
    }
}
