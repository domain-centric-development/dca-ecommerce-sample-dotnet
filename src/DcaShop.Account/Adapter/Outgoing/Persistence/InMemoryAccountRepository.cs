using System.Collections.Concurrent;
using DcaShop.Account.Application.Shared;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Adapter.Outgoing.Persistence;

/// <summary>
/// In-memory account store with indexes by email and by linked identity — demo stand-in for a real database
/// adapter; it hands out the stored instances, so it shares the aggregate between requests (sample ADR-001).
/// </summary>
public sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly ConcurrentDictionary<AccountId, Domain.Model.Account> _accounts = new();
    private readonly ConcurrentDictionary<string, AccountId> _byEmail = new();
    private readonly ConcurrentDictionary<UserId, AccountId> _byLinkedUserId = new();

    public Task<Domain.Model.Account?> FindByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.TryGetValue(id, out var account) ? account : null);

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

        // The email is the login credential and can change, so a stale index entry would keep answering under the
        // old address; drop whatever this account was indexed under before re-indexing it.
        if (_accounts.TryGetValue(aggregate.Id, out var stored) && stored.Email.Value != aggregate.Email.Value)
        {
            _byEmail.TryRemove(stored.Email.Value, out _);
        }

        Claim(_byEmail, aggregate.Email.Value, aggregate.Id, "Email address");
        Claim(_byLinkedUserId, aggregate.LinkedUserId, aggregate.Id, "Linked user id");

        _accounts[aggregate.Id] = aggregate;
        return Task.FromResult(aggregate);
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
            _byEmail.TryRemove(removed.Email.Value, out _);
            _byLinkedUserId.TryRemove(removed.LinkedUserId, out _);
        }

        return Task.CompletedTask;
    }

    private Domain.Model.Account? Lookup<TKey>(ConcurrentDictionary<TKey, AccountId> index, TKey key)
        where TKey : notnull =>
        index.TryGetValue(key, out var id) && _accounts.TryGetValue(id, out var account) ? account : null;
}
