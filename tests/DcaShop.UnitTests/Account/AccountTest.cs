using DcaShop.Account.Adapter.Outgoing.Security;
using DcaShop.Account.Domain.Event;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Account;

public sealed class AccountTest
{
    private static readonly TestPasswordHasher Hasher = new();
    private const string ValidPassword = "Secret123";

    private static DcaShop.Account.Domain.Model.Account NewAccount(string email = "jane@example.com") =>
        DcaShop.Account.Domain.Model.Account.Register(
            Email.Of(email),
            Owner.Of("Jane", "Doe", new DateOnly(1990, 5, 17)),
            ValidPassword,
            UserId.Of("visitor-1"),
            Hasher);

    [Fact]
    public void RegistrationKeepsTheVisitorIdentitySoTheCartSurvives()
    {
        var account = NewAccount();

        Assert.Equal(UserId.Of("visitor-1"), account.LinkedUserId);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Contains(Role.Customer, account.Roles);
    }

    [Fact]
    public void RegistrationRaisesRegisteredAndLinkedToIdentity()
    {
        var account = NewAccount();

        Assert.Collection(
            account.DomainEvents,
            e => Assert.IsType<AccountRegistered>(e),
            e => Assert.IsType<AccountLinkedToIdentity>(e));
    }

    [Fact]
    public void ThePasswordIsCheckedThroughTheAggregate()
    {
        var account = NewAccount();

        Assert.True(account.CheckPassword(ValidPassword, Hasher));
        Assert.False(account.CheckPassword("Wrong123x", Hasher));
    }

    [Fact]
    public void TheRealHasherNeverStoresThePlaintext()
    {
        // The other cases use a reversible test double; this one uses the adapter the shop actually runs, because
        // "never stored as plaintext" is a promise about that adapter, not about the aggregate.
        var bcrypt = new BcryptPasswordHasher();
        var account = DcaShop.Account.Domain.Model.Account.Register(
            Email.Of("jane@example.com"),
            Owner.Of("Jane", "Doe", new DateOnly(1990, 5, 17)),
            ValidPassword,
            UserId.Of("visitor-1"),
            bcrypt);

        Assert.DoesNotContain(ValidPassword, account.Password.Hash, StringComparison.Ordinal);
        Assert.True(account.CheckPassword(ValidPassword, bcrypt));
        Assert.False(account.CheckPassword("Wrong123x", bcrypt));
    }

    [Fact]
    public void RecordingALoginStampsTheTimeAndRaisesLoggedIn()
    {
        var account = NewAccount();
        account.ClearDomainEvents();

        account.RecordLogin();

        Assert.NotNull(account.LastLoginAt);
        Assert.IsType<AccountLoggedIn>(Assert.Single(account.DomainEvents));
    }

    [Fact]
    public void ASuspendedAccountCannotLogIn()
    {
        var account = NewAccount();
        account.Suspend();

        Assert.False(account.Status.CanLogin());
        Assert.Throws<InvalidOperationException>(account.RecordLogin);
    }

    [Fact]
    public void ReactivatingRestoresLogin()
    {
        var account = NewAccount();
        account.Suspend();

        account.Reactivate();

        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.True(account.Status.CanLogin());
    }

    [Fact]
    public void AClosedAccountRefusesEveryChange()
    {
        var account = NewAccount();
        account.Close();

        Assert.True(account.Status.IsTerminal());
        Assert.Throws<InvalidOperationException>(() => account.ChangePassword("Another123", Hasher));
        Assert.Throws<InvalidOperationException>(() => account.ChangeEmail(Email.Of("new@example.com")));
        Assert.Throws<InvalidOperationException>(() => account.ChangeOwnerDateOfBirth(new DateOnly(1991, 1, 1)));
        Assert.Throws<InvalidOperationException>(account.Close);
    }

    [Fact]
    public void ChangingToTheSameEmailRaisesNothing()
    {
        var account = NewAccount();
        account.ClearDomainEvents();

        account.ChangeEmail(Email.Of("jane@example.com"));

        Assert.Empty(account.DomainEvents);
    }

    [Fact]
    public void ChangingTheEmailCarriesThePreviousAddress()
    {
        var account = NewAccount();
        account.ClearDomainEvents();

        account.ChangeEmail(Email.Of("new@example.com"));

        var changed = Assert.IsType<AccountEmailChanged>(Assert.Single(account.DomainEvents));
        Assert.Equal("jane@example.com", changed.PreviousEmail.Value);
        Assert.Equal("new@example.com", changed.NewEmail.Value);
    }

    [Fact]
    public void CorrectingTheDateOfBirthLeavesTheNameUntouched()
    {
        var account = NewAccount();
        account.ClearDomainEvents();

        account.ChangeOwnerDateOfBirth(new DateOnly(1991, 6, 18));

        Assert.Equal("Jane", account.Owner.FirstName);
        Assert.Equal("Doe", account.Owner.LastName);
        var changed = Assert.IsType<AccountOwnerDateOfBirthChanged>(Assert.Single(account.DomainEvents));
        Assert.Equal(new DateOnly(1990, 5, 17), changed.PreviousDateOfBirth);
        Assert.Equal(new DateOnly(1991, 6, 18), changed.NewDateOfBirth);
    }

    [Fact]
    public void ChangingThePasswordReplacesTheHash()
    {
        var account = NewAccount();
        account.ClearDomainEvents();

        account.ChangePassword("Another123", Hasher);

        Assert.True(account.CheckPassword("Another123", Hasher));
        Assert.False(account.CheckPassword(ValidPassword, Hasher));
        Assert.IsType<AccountPasswordChanged>(Assert.Single(account.DomainEvents));
    }
}
