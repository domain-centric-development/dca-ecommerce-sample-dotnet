using DcaShop.Account.Domain.Model;
using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Account.Application.RegisterAccount;

/// <summary>Raised when a registration uses an email address another account already holds.</summary>
/// <remarks>
/// Uniqueness across all accounts is nothing a single account can check, so the rule belongs to the use case
/// and its store, not to the model.
/// </remarks>
public sealed class EmailAlreadyRegisteredException : UseCaseException
{
    public EmailAlreadyRegisteredException(Email email)
        : base($"Email is already registered: {email.Value}")
    {
        Email = email;
    }

    public Email Email { get; }
}
