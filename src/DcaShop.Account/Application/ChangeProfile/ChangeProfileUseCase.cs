using DcaShop.Account.Application.Shared;
using DcaShop.Account.Domain.Model;
using DcaShop.Account.Domain.Specification;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.Extensions.Logging;

namespace DcaShop.Account.Application.ChangeProfile;

/// <summary>Changes the login address and the owner's date of birth of the current account.</summary>
public sealed class ChangeProfileUseCase : IChangeProfileInputPort
{
    private const string EmailAlreadyInUseMessage = "This email address is already registered";

    private readonly IAccountRepository _accounts;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;
    private readonly ILogger<ChangeProfileUseCase> _logger;

    public ChangeProfileUseCase(
        IAccountRepository accounts,
        IDomainEventPublisher events,
        ITransactionBoundary transactionBoundary,
        ILogger<ChangeProfileUseCase> logger)
    {
        _accounts = accounts;
        _events = events;
        _transactionBoundary = transactionBoundary;
        _logger = logger;
    }

    public async Task<ChangeProfileResult> ExecuteAsync(
        ChangeProfileCommand command, CancellationToken cancellationToken = default)
    {
        var userId = UserId.Of(command.UserId);

        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var account = await _accounts.FindByLinkedUserIdAsync(userId, ct).ConfigureAwait(false);
                if (account is null || !account.Status.CanLogin())
                {
                    _logger.LogWarning("Profile change attempt without an accessible account");
                    return ChangeProfileResult.AccountNotAccessible();
                }

                Email newEmail;
                try
                {
                    newEmail = Email.Of(command.Email);

                    // Both values are checked before either is applied, so a rejection leaves the profile whole.
                    UsableDateOfBirth.Rule.RequireSatisfiedBy(command.DateOfBirth);
                }
                catch (ArgumentException e)
                {
                    _logger.LogDebug("Profile change rejected: {Reason}", e.Message);
                    return ChangeProfileResult.InputRejected(e.Message);
                }

                if (newEmail != account.Email
                    && await _accounts.ExistsByEmailAsync(newEmail, ct).ConfigureAwait(false))
                {
                    _logger.LogDebug("Profile change rejected: email already in use");
                    return ChangeProfileResult.EmailAlreadyInUse(EmailAlreadyInUseMessage);
                }

                account.ChangeEmail(newEmail);
                account.ChangeOwnerDateOfBirth(command.DateOfBirth!.Value);
                await _accounts.SaveAsync(account, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(account, ct).ConfigureAwait(false);

                _logger.LogInformation("Profile changed for account {AccountId}", account.Id);
                return ChangeProfileResult.Changed(
                    new ChangeProfileResult.ProfileView(account.Email.Value, account.Owner.DateOfBirth));
            },
            cancellationToken).ConfigureAwait(false);
    }
}
