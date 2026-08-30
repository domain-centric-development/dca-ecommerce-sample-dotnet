using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Account.Application.Shared;

/// <summary>Persists and reconstitutes accounts.</summary>
public interface IAccountRepository : IRepository<Domain.Model.Account, AccountId>
{
    /// <summary>Finds the account whose login credential is this address.</summary>
    Task<Domain.Model.Account?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>Finds the account linked to a cross-context identity.</summary>
    Task<Domain.Model.Account?> FindByLinkedUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Whether the address is already taken — the uniqueness rule the aggregate cannot decide alone.</summary>
    async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        await FindByEmailAsync(email, cancellationToken).ConfigureAwait(false) is not null;
}
