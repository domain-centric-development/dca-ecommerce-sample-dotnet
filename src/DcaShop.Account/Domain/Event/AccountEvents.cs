using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Event;

/// <summary>
/// A new account has been registered. The owner travels with the event so a consumer (welcome mail, analytics)
/// can address the person by name without querying back.
/// </summary>
public sealed record AccountRegistered(
    Guid EventId,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    Email Email,
    Owner Owner,
    UserId LinkedUserId) : IDomainEvent
{
    public static AccountRegistered Now(AccountId accountId, Email email, Owner owner, UserId linkedUserId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, accountId, email, owner, linkedUserId);
}

/// <summary>
/// The cross-context <see cref="UserId"/> has been linked to an account — it signals to other contexts that the
/// identity they already key their data on now belongs to a registered user.
/// </summary>
public sealed record AccountLinkedToIdentity(
    Guid EventId,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    UserId UserId) : IDomainEvent
{
    public static AccountLinkedToIdentity Now(AccountId accountId, UserId userId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, accountId, userId);
}

/// <summary>A user has successfully logged in to their account.</summary>
public sealed record AccountLoggedIn(Guid EventId, DateTimeOffset OccurredOn, AccountId AccountId) : IDomainEvent
{
    public static AccountLoggedIn Now(AccountId accountId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, accountId);
}

/// <summary>The password of an account has been changed.</summary>
public sealed record AccountPasswordChanged(Guid EventId, DateTimeOffset OccurredOn, AccountId AccountId) : IDomainEvent
{
    public static AccountPasswordChanged Now(AccountId accountId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, accountId);
}

/// <summary>
/// The email address of an account has been changed; it carries the previous and the new address. Since the email
/// is also the login credential, the identity token is re-issued afterwards.
/// </summary>
public sealed record AccountEmailChanged(
    Guid EventId,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    Email PreviousEmail,
    Email NewEmail) : IDomainEvent
{
    public static AccountEmailChanged Now(AccountId accountId, Email previousEmail, Email newEmail) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, accountId, previousEmail, newEmail);
}

/// <summary>
/// The date of birth of an account's owner has been corrected; it carries the previous and the new date, because
/// a correction is only interpretable against the value it replaced. There is deliberately no counterpart for the
/// owner's name, because no operation changes it.
/// </summary>
public sealed record AccountOwnerDateOfBirthChanged(
    Guid EventId,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    DateOnly PreviousDateOfBirth,
    DateOnly NewDateOfBirth) : IDomainEvent
{
    public static AccountOwnerDateOfBirthChanged Now(
        AccountId accountId, DateOnly previousDateOfBirth, DateOnly newDateOfBirth) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, accountId, previousDateOfBirth, newDateOfBirth);
}

/// <summary>An account has been temporarily blocked and can no longer log in.</summary>
public sealed record AccountSuspended(Guid EventId, DateTimeOffset OccurredOn, AccountId AccountId) : IDomainEvent
{
    public static AccountSuspended Now(AccountId accountId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, accountId);
}

/// <summary>A previously suspended account has been re-enabled.</summary>
public sealed record AccountReactivated(Guid EventId, DateTimeOffset OccurredOn, AccountId AccountId) : IDomainEvent
{
    public static AccountReactivated Now(AccountId accountId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, accountId);
}

/// <summary>An account has been permanently closed; the terminal state of the lifecycle.</summary>
public sealed record AccountClosed(Guid EventId, DateTimeOffset OccurredOn, AccountId AccountId) : IDomainEvent
{
    public static AccountClosed Now(AccountId accountId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, accountId);
}
