using DcaShop.Account.Application.Shared;
using DcaShop.Account.Domain.Gateway;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.Extensions.Logging;

namespace DcaShop.Account.Application.ChangePassword;

/// <summary>Changes the password of the account the current session belongs to.</summary>
public sealed class ChangePasswordUseCase : IChangePasswordInputPort
{
    private const string CurrentPasswordInvalidMessage = "Current password is not correct";

    private readonly IAccountRepository _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;
    private readonly ILogger<ChangePasswordUseCase> _logger;

    public ChangePasswordUseCase(
        IAccountRepository accounts,
        IPasswordHasher passwordHasher,
        IDomainEventPublisher events,
        ITransactionBoundary transactionBoundary,
        ILogger<ChangePasswordUseCase> logger)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _events = events;
        _transactionBoundary = transactionBoundary;
        _logger = logger;
    }

    public async Task<ChangePasswordResult> ExecuteAsync(
        ChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        var userId = UserId.Of(command.UserId);

        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var account = await _accounts.FindByLinkedUserIdAsync(userId, ct).ConfigureAwait(false);
                if (account is null || !account.Status.CanLogin())
                {
                    _logger.LogWarning("Password change attempt without an accessible account");
                    return ChangePasswordResult.AccountNotAccessible();
                }

                if (!account.CheckPassword(command.CurrentPassword, _passwordHasher))
                {
                    _logger.LogWarning("Password change attempt with a wrong current password");
                    return ChangePasswordResult.CurrentPasswordInvalid(CurrentPasswordInvalidMessage);
                }

                // Only the strength decision may become NewPasswordRejected. Wrapping the whole of ChangePassword
                // would also catch an ArgumentException from the hasher (BCrypt rejects input over 72 bytes) or
                // from the HashedPassword factory (blank hash), and the controller renders that message to the
                // user verbatim — mislabelling an adapter fault as a password rule.
                try
                {
                    HashedPassword.ValidatePasswordStrength(command.NewPassword);
                }
                catch (ArgumentException e)
                {
                    _logger.LogDebug("Password change rejected: {Reason}", e.Message);
                    return ChangePasswordResult.NewPasswordRejected(e.Message);
                }

                account.ChangePassword(command.NewPassword, _passwordHasher);
                await _accounts.SaveAsync(account, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(account, ct).ConfigureAwait(false);

                _logger.LogInformation("Password changed for account {AccountId}", account.Id);
                return ChangePasswordResult.Changed();
            },
            cancellationToken).ConfigureAwait(false);
    }
}
