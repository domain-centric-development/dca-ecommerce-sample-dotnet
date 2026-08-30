using DcaShop.Account.Domain.Event;
using DcaShop.Account.Domain.Gateway;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Model;

/// <summary>
/// A registered user account with its credentials and profile. Aggregate root of the Account context.
/// </summary>
/// <remarks>
/// <para>
/// The account links the cross-context <see cref="UserId"/> — the identity the cart and the checkout session are
/// keyed on — to its own <see cref="AccountId"/>. Registration preserves the incoming <c>UserId</c> unchanged,
/// which is what lets a guest who registers mid-checkout keep their cart.
/// </para>
/// <para>
/// Its rules: the email is the unique login credential, the password must satisfy the policy of
/// <see cref="HashedPassword"/>, a suspended or closed account cannot log in, and the owner's name is captured
/// at registration and never changes — only the date of birth can be corrected.
/// </para>
/// </remarks>
public sealed class Account : AggregateRootBase<Account, AccountId>
{
    private readonly HashSet<string> _roles;

    private Account(
        AccountId id,
        Email email,
        Owner owner,
        UserId linkedUserId,
        HashedPassword password,
        IEnumerable<string> roles,
        DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;

        // Every account belongs to somebody: an owner-less account could never satisfy the rule that the owner's
        // name is fixed, because there would be no name to fix.
        Owner = owner ?? throw new ArgumentNullException(nameof(owner), "An account must have an owner");
        LinkedUserId = linkedUserId;
        Password = password;
        Status = AccountStatus.Active;
        _roles = [.. roles];
        CreatedAt = createdAt;
    }

    public override AccountId Id { get; }

    public Email Email { get; private set; }

    public Owner Owner { get; private set; }

    /// <summary>The cross-context identity this account belongs to. Fixed once the account exists.</summary>
    public UserId LinkedUserId { get; }

    /// <summary>
    /// The stored hash. For authentication prefer <see cref="CheckPassword"/>, which keeps the verification step
    /// inside the aggregate; this accessor exists for persistence adapters.
    /// </summary>
    public HashedPassword Password { get; private set; }

    public AccountStatus Status { get; private set; }

    public IReadOnlySet<string> Roles => _roles;

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>
    /// Registers a new account under the <see cref="UserId"/> the visitor already carries, so cart and checkout
    /// session survive the guest-to-account transition. Registers <see cref="AccountRegistered"/> and
    /// <see cref="AccountLinkedToIdentity"/>.
    /// </summary>
    public static Account Register(
        Email email, Owner owner, string plainPassword, UserId currentUserId, IPasswordHasher passwordHasher)
    {
        var hashedPassword = HashedPassword.FromPlaintext(plainPassword, passwordHasher);
        var accountId = AccountId.Generate();

        var account = new Account(
            accountId, email, owner, currentUserId, hashedPassword, [Role.Customer], DateTimeOffset.UtcNow);

        account.RegisterEvent(AccountRegistered.Now(accountId, email, owner, currentUserId));
        account.RegisterEvent(AccountLinkedToIdentity.Now(accountId, currentUserId));
        return account;
    }

    /// <summary>Rebuilds an account from storage. Registers no events.</summary>
    public static Account Reconstitute(
        AccountId id,
        Email email,
        Owner owner,
        UserId linkedUserId,
        HashedPassword password,
        AccountStatus status,
        IEnumerable<string> roles,
        DateTimeOffset createdAt,
        DateTimeOffset? lastLoginAt) =>
        new(id, email, owner, linkedUserId, password, roles, createdAt)
        {
            Status = status,
            LastLoginAt = lastLoginAt,
        };

    /// <summary>Verifies a plaintext password against the stored hash via the domain gateway.</summary>
    public bool CheckPassword(string plainPassword, IPasswordHasher passwordHasher) =>
        Password.Matches(plainPassword, passwordHasher);

    /// <summary>Records a successful login and registers <see cref="AccountLoggedIn"/>.</summary>
    public void RecordLogin()
    {
        if (!Status.CanLogin())
        {
            throw new InvalidOperationException($"Cannot login with account status: {Status}");
        }

        LastLoginAt = DateTimeOffset.UtcNow;
        RegisterEvent(AccountLoggedIn.Now(Id));
    }

    /// <summary>Validates and hashes a new password, then registers <see cref="AccountPasswordChanged"/>.</summary>
    public void ChangePassword(string newPlainPassword, IPasswordHasher passwordHasher)
    {
        if (Status.IsTerminal())
        {
            throw new InvalidOperationException("Cannot change password on closed account");
        }

        Password = HashedPassword.FromPlaintext(newPlainPassword, passwordHasher);
        RegisterEvent(AccountPasswordChanged.Now(Id));
    }

    /// <summary>
    /// Changes the email address, which is also the login credential. Registers
    /// <see cref="AccountEmailChanged"/> only when the new address differs from the stored one; uniqueness
    /// across accounts is decided outside the aggregate.
    /// </summary>
    public void ChangeEmail(Email newEmail)
    {
        if (Status.IsTerminal())
        {
            throw new InvalidOperationException("Cannot change email on closed account");
        }

        if (Email == newEmail)
        {
            return;
        }

        var previousEmail = Email;
        Email = newEmail;
        RegisterEvent(AccountEmailChanged.Now(Id, previousEmail, newEmail));
    }

    /// <summary>
    /// Corrects the date of birth of the owner. The name is not touched: the corrected owner is derived via
    /// <see cref="Owner.WithDateOfBirth"/>, which carries both names over.
    /// </summary>
    public void ChangeOwnerDateOfBirth(DateOnly newDateOfBirth)
    {
        if (Status.IsTerminal())
        {
            throw new InvalidOperationException("Cannot change the date of birth on closed account");
        }

        if (Owner.DateOfBirth == newDateOfBirth)
        {
            return;
        }

        var previousDateOfBirth = Owner.DateOfBirth;
        Owner = Owner.WithDateOfBirth(newDateOfBirth);
        RegisterEvent(AccountOwnerDateOfBirthChanged.Now(Id, previousDateOfBirth, newDateOfBirth));
    }

    /// <summary>Temporarily blocks the account.</summary>
    public void Suspend()
    {
        if (Status.IsTerminal())
        {
            throw new InvalidOperationException("Cannot suspend closed account");
        }

        Status = AccountStatus.Suspended;
        RegisterEvent(AccountSuspended.Now(Id));
    }

    /// <summary>Re-enables a suspended account.</summary>
    public void Reactivate()
    {
        if (Status != AccountStatus.Suspended)
        {
            throw new InvalidOperationException("Can only reactivate suspended accounts");
        }

        Status = AccountStatus.Active;
        RegisterEvent(AccountReactivated.Now(Id));
    }

    /// <summary>Ends the account permanently.</summary>
    public void Close()
    {
        if (Status.IsTerminal())
        {
            throw new InvalidOperationException("Account is already closed");
        }

        Status = AccountStatus.Closed;
        RegisterEvent(AccountClosed.Now(Id));
    }

    public void AddRole(string role) => _roles.Add(role);

    public void RemoveRole(string role) => _roles.Remove(role);
}
