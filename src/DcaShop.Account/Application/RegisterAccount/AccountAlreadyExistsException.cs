using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Account.Application.RegisterAccount;

/// <summary>Raised when the signed-in user already has an account and would register a second one.</summary>
/// <remarks>
/// One account per user, checked where the identity of the caller is known — in the use case, which is the
/// only layer that sees both the request and the store.
/// </remarks>
public sealed class AccountAlreadyExistsException : UseCaseException
{
    public AccountAlreadyExistsException(UserId userId)
        : base("User already has an account")
    {
        UserId = userId;
    }

    public UserId UserId { get; }
}
