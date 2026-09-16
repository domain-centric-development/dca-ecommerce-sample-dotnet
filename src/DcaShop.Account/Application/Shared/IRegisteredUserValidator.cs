using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Account.Application.Shared;

/// <summary>
/// Answers whether an account still exists for an identity. A session token is self-contained, so it outlives
/// the account it names: without this check a deleted account would leave a session that still validates and
/// still carries roles.
/// </summary>
public interface IRegisteredUserValidator : IOutputPort
{
    Task<bool> ExistsForUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
}
