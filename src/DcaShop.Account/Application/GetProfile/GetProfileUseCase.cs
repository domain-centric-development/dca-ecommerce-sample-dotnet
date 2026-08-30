using DcaShop.Account.Application.Shared;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Application.GetProfile;

/// <summary>Reads the profile of the account the current session belongs to.</summary>
public sealed class GetProfileUseCase : IGetProfileInputPort
{
    private readonly IAccountRepository _accounts;

    public GetProfileUseCase(IAccountRepository accounts) => _accounts = accounts;

    public async Task<GetProfileResult> ExecuteAsync(
        GetProfileQuery query, CancellationToken cancellationToken = default)
    {
        var account = await _accounts
            .FindByLinkedUserIdAsync(UserId.Of(query.UserId), cancellationToken)
            .ConfigureAwait(false);

        if (account is null || !account.Status.CanLogin())
        {
            return GetProfileResult.NotFound();
        }

        return new GetProfileResult(new GetProfileResult.ProfileView(
            account.Email.Value,
            account.Owner.FirstName,
            account.Owner.LastName,
            account.Owner.DateOfBirth));
    }
}
