using DcaShop.Account.Adapter.Outgoing.Persistence;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Account;

/// <summary>
/// An email address belongs to one account, and so does a visitor identity. The store claims both the way the
/// unique columns of a schema do, so a check made before the save cannot be overtaken between the two.
/// </summary>
public sealed class AccountUniquenessTest
{
    private readonly InMemoryAccountRepository _accounts = new();

    [Fact]
    public async Task ASecondAccountUnderTheSameAddressIsRefused()
    {
        await _accounts.SaveAsync(AccountOf("ada@example.com", UserId.GenerateAnonymous()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _accounts.SaveAsync(AccountOf("ada@example.com", UserId.GenerateAnonymous())));
    }

    [Fact]
    public async Task ASecondAccountForTheSameVisitorIsRefused()
    {
        var visitor = UserId.GenerateAnonymous();
        await _accounts.SaveAsync(AccountOf("first@example.com", visitor));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _accounts.SaveAsync(AccountOf("second@example.com", visitor)));
    }

    [Fact]
    public async Task SavingTheSameAccountAgainKeepsItsAddress()
    {
        var account = await _accounts.SaveAsync(AccountOf("grace@example.com", UserId.GenerateAnonymous()));

        await _accounts.SaveAsync(account);

        Assert.Equal(account.Id, (await _accounts.FindByEmailAsync(Email.Of("grace@example.com")))!.Id);
    }

    private static DcaShop.Account.Domain.Model.Account AccountOf(string email, UserId visitor) =>
        DcaShop.Account.Domain.Model.Account.Register(
            Email.Of(email),
            Owner.Of("Ada", "Lovelace", new DateOnly(1815, 12, 10)),
            "Secret123",
            visitor,
            new TestPasswordHasher());
}