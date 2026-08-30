using DcaShop.Account.Application.Shared;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Application.GetAccountOverview;

/// <summary>Reads the landing page of the account the current session belongs to.</summary>
public sealed class GetAccountOverviewUseCase : IGetAccountOverviewInputPort
{
    private readonly IAccountRepository _accounts;

    public GetAccountOverviewUseCase(IAccountRepository accounts) => _accounts = accounts;

    public async Task<GetAccountOverviewResult> ExecuteAsync(
        GetAccountOverviewQuery query, CancellationToken cancellationToken = default)
    {
        var account = await _accounts
            .FindByLinkedUserIdAsync(UserId.Of(query.UserId), cancellationToken)
            .ConfigureAwait(false);

        if (account is null || !account.Status.CanLogin())
        {
            return GetAccountOverviewResult.NotFound();
        }

        return new GetAccountOverviewResult(
            new GetAccountOverviewResult.AccountOverview(account.Email.Value, account.LastLoginAt));
    }
}
