using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Account.Application.RegisterAccount;

/// <summary>
/// What a registration came to: registered, or one of the refusals a caller answers. Each refusal is decided before
/// the account is written.
/// </summary>
[GenerateOneOf]
public sealed partial class RegisterAccountResult
    : OneOfBase<
        RegisterAccountResult.Registered,
        RegisterAccountResult.EmailAlreadyRegistered,
        RegisterAccountResult.AccountAlreadyExists,
        RegisterAccountResult.PasswordTooWeak>
{
    /// <summary>The registered account, as the adapter needs it to mint a session token.</summary>
    public sealed record Registered(string AccountId, string UserId, string Email, IReadOnlySet<Role> Roles);

    /// <summary>
    /// Another account already holds the address. Uniqueness across all accounts is nothing a single account can
    /// check, so the refusal belongs to the use case and its store, not to the model.
    /// </summary>
    public sealed record EmailAlreadyRegistered(Email Email) : IRefusal;

    /// <summary>
    /// The signed-in user already has an account and would register a second one. One account per user, checked where
    /// the identity of the caller is known — in the use case.
    /// </summary>
    public sealed record AccountAlreadyExists(UserId UserId) : IRefusal;

    /// <summary>The password does not satisfy the password policy — the model's rule, caught once by the use case.</summary>
    /// <param name="BrokenRule">The part of the policy the password breaks.</param>
    public sealed record PasswordTooWeak(PasswordRule BrokenRule) : IRefusal;
}