using System.Collections.Concurrent;

using DcaShop.Account.Application.Shared;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Adapter.Outgoing.Persistence;

/// <summary>
/// In-memory account store with indexes by email and by linked identity — the unit tests' stand-in for the database
/// adapter. It stores and hands out copies, as a database does: a change nobody saved is not seen by the next load.
/// </summary>
public sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly ConcurrentDictionary<AccountId, Domain.Model.Account> _accounts = new();
    private readonly ConcurrentDictionary<string, AccountId> _byEmail = new();
    private readonly ConcurrentDictionary<UserId, AccountId> _byLinkedUserId = new();
    private readonly ConcurrentDictionary<AccountId, string> _indexedEmailByAccount = new();

    public Task<Domain.Model.Account?> FindByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.TryGetValue(id, out var account) ? Copy(account) : null);

    public Task<Domain.Model.Account?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        return Task.FromResult(Lookup(_byEmail, email.Value));
    }

    public Task<Domain.Model.Account?> FindByLinkedUserIdAsync(
        UserId userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Lookup(_byLinkedUserId, userId));

    public Task<Domain.Model.Account> SaveAsync(
        Domain.Model.Account aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        Claim(_byEmail, aggregate.Email.Value, aggregate.Id, "Email address");
        Claim(_byLinkedUserId, aggregate.LinkedUserId, aggregate.Id, "Linked user id");
        ReleasePreviousEmail(aggregate);

        _accounts[aggregate.Id] = Copy(aggregate);
        return Task.FromResult(aggregate);
    }

    private static Domain.Model.Account Copy(Domain.Model.Account account) => Domain.Model.Account.Reconstitute(
        account.Id, account.Email, account.Owner, account.LinkedUserId, account.Password, account.Status, account.Roles,
        account.CreatedAt, account.LastLoginAt);

    /// <summary>
    /// The email is the login credential and can change, so a stale index entry would keep answering under the old
    /// address; the address each account is indexed under is kept apart and released once the new one is claimed.
    /// </summary>
    private void ReleasePreviousEmail(Domain.Model.Account account)
    {
        if (_indexedEmailByAccount.TryGetValue(account.Id, out var previous) && previous != account.Email.Value)
        {
            _byEmail.TryRemove(KeyValuePair.Create(previous, account.Id));
        }

        _indexedEmailByAccount[account.Id] = account.Email.Value;
    }

    /// <summary>Claims a unique value for one account, or refuses when somebody else holds it.</summary>
    /// <exception cref="InvalidOperationException">Another account already holds the value.</exception>
    private static void Claim<TKey>(ConcurrentDictionary<TKey, AccountId> index, TKey value, AccountId accountId, string what)
        where TKey : notnull
    {
        var holder = index.GetOrAdd(value, accountId);
        if (holder != accountId)
        {
            throw new InvalidOperationException($"{what} {value} already belongs to another account");
        }
    }

    public Task DeleteByIdAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        if (_accounts.TryRemove(id, out var removed))
        {
            if (_indexedEmailByAccount.TryRemove(id, out var indexed))
            {
                _byEmail.TryRemove(KeyValuePair.Create(indexed, id));
            }

            _byLinkedUserId.TryRemove(removed.LinkedUserId, out _);
        }

        return Task.CompletedTask;
    }

    private Domain.Model.Account? Lookup<TKey>(ConcurrentDictionary<TKey, AccountId> index, TKey key)
        where TKey : notnull =>
        index.TryGetValue(key, out var id) && _accounts.TryGetValue(id, out var account) ? Copy(account) : null;
}