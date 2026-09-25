using DcaShop.Account.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Application.IsAccountRegistered;

/// <summary>
/// Answers whether an account is registered for a user id. Read-only: an account exists or it does not. A session
/// token that names an account nobody can find is stale — whether the account was deleted or the store never kept
/// it — and the authentication handler downgrades such a session to an anonymous visitor. Whether the account may
/// currently log in is a different question, answered where the login happens.
/// </summary>
public sealed class IsAccountRegisteredUseCase : IIsAccountRegisteredInputPort
{
    private readonly IAccountRepository _accounts;

    public IsAccountRegisteredUseCase(IAccountRepository accounts) => _accounts = accounts;

    public async Task<IsAccountRegisteredResult> ExecuteAsync(
        IsAccountRegisteredQuery query, CancellationToken cancellationToken = default)
    {
        var account = await _accounts
            .FindByLinkedUserIdAsync(UserId.Of(query.UserId), cancellationToken)
            .ConfigureAwait(false);

        return new IsAccountRegisteredResult(account is not null);
    }
}