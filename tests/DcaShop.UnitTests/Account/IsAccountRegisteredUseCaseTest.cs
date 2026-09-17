using DcaShop.Account.Application.IsAccountRegistered;
using DcaShop.Account.Application.Shared;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Account;

/// <summary>
/// Pins the one fact the authentication handler needs: whether the account a session token names still exists.
/// Status is deliberately not part of the answer — a suspended account is registered, it just cannot log in — and
/// the query performs no write. Mirrors the Java <c>IsAccountRegisteredUseCaseTest</c>.
/// </summary>
public sealed class IsAccountRegisteredUseCaseTest
{
    private const string UserIdValue = "user-4711";

    private readonly TestAccountRepository _accounts = new();
    private readonly IsAccountRegisteredUseCase _isAccountRegistered;

    public IsAccountRegisteredUseCaseTest() => _isAccountRegistered = new IsAccountRegisteredUseCase(_accounts);

    [Theory]
    [InlineData(AccountStatus.Active)]
    [InlineData(AccountStatus.Suspended)]
    [InlineData(AccountStatus.Closed)]
    public async Task ExistingAccountIsRegistered(AccountStatus status)
    {
        _accounts.Store(AccountWith(status));

        var result = await _isAccountRegistered.ExecuteAsync(new IsAccountRegisteredQuery(UserIdValue));

        Assert.True(result.Registered);
        Assert.False(_accounts.Saved, "a query must not write");
    }

    [Fact]
    public async Task UnknownUserIsNotRegistered()
    {
        var result = await _isAccountRegistered.ExecuteAsync(new IsAccountRegisteredQuery("user-nobody"));

        Assert.False(result.Registered);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void QueryRefusesBlankUserId(string? userId) =>
        Assert.Throws<ArgumentException>(() => new IsAccountRegisteredQuery(userId!));

    private static DcaShop.Account.Domain.Model.Account AccountWith(AccountStatus status) =>
        DcaShop.Account.Domain.Model.Account.Reconstitute(
            AccountId.Of("00000000-0000-0000-0000-000000000001"),
            Email.Of("jane.doe@example.com"),
            Owner.Of("Jane", "Doe", new DateOnly(1990, 5, 17)),
            UserId.Of(UserIdValue),
            HashedPassword.FromHash("hashed:OldPassw0rd"),
            status,
            new[] { "CUSTOMER" },
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-07-31T08:15:30Z"));

    private sealed class TestAccountRepository : IAccountRepository
    {
        private readonly Dictionary<AccountId, DcaShop.Account.Domain.Model.Account> _accounts = new();

        public bool Saved { get; private set; }

        public void Store(DcaShop.Account.Domain.Model.Account account) => _accounts[account.Id] = account;

        public Task<DcaShop.Account.Domain.Model.Account?> FindByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.GetValueOrDefault(id));

        public Task<DcaShop.Account.Domain.Model.Account> SaveAsync(DcaShop.Account.Domain.Model.Account aggregate, CancellationToken cancellationToken = default)
        {
            Saved = true;
            _accounts[aggregate.Id] = aggregate;
            return Task.FromResult(aggregate);
        }

        public Task DeleteByIdAsync(AccountId id, CancellationToken cancellationToken = default)
        {
            _accounts.Remove(id);
            return Task.CompletedTask;
        }

        public Task<DcaShop.Account.Domain.Model.Account?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.Values.FirstOrDefault(account => account.Email.Equals(email)));

        public Task<DcaShop.Account.Domain.Model.Account?> FindByLinkedUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.Values.FirstOrDefault(account => account.LinkedUserId.Equals(userId)));
    }
}
