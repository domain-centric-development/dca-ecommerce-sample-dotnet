using DcaShop.Account.Application.Shared;
using DcaShop.Account.Domain.Gateway;
using DcaShop.Account.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.Extensions.Logging;

namespace DcaShop.Account.Application.AuthenticateAccount;

/// <summary>
/// Verifies credentials and records the login. It answers a wrong address, a wrong password and a malformed
/// address with the same message, so a caller cannot use it to enumerate accounts.
/// </summary>
public sealed class AuthenticateAccountUseCase : IAuthenticateAccountInputPort
{
    private const string GenericError = "Invalid email or password";

    private readonly IAccountRepository _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;
    private readonly ILogger<AuthenticateAccountUseCase> _logger;

    public AuthenticateAccountUseCase(
        IAccountRepository accounts,
        IPasswordHasher passwordHasher,
        IDomainEventPublisher events,
        ITransactionBoundary transactionBoundary,
        ILogger<AuthenticateAccountUseCase> logger)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _events = events;
        _transactionBoundary = transactionBoundary;
        _logger = logger;
    }

    public async Task<AuthenticateAccountResult> ExecuteAsync(
        AuthenticateAccountCommand command, CancellationToken cancellationToken = default)
    {
        Email email;
        try
        {
            email = Email.Of(command.Email);
        }
        catch (ArgumentException)
        {
            _logger.LogDebug("Invalid email format during login attempt");
            return AuthenticateAccountResult.Failed(GenericError);
        }

        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var account = await _accounts.FindByEmailAsync(email, ct).ConfigureAwait(false);
                if (account is null)
                {
                    _logger.LogDebug("Login attempt for non-existent email");
                    return AuthenticateAccountResult.Failed(GenericError);
                }

                if (!account.CheckPassword(command.Password, _passwordHasher))
                {
                    _logger.LogWarning("Failed login attempt for account {AccountId}", account.Id);
                    return AuthenticateAccountResult.Failed(GenericError);
                }

                if (!account.Status.CanLogin())
                {
                    _logger.LogWarning(
                        "Login attempt for {Status} account {AccountId}", account.Status, account.Id);
                    return AuthenticateAccountResult.Failed(
                        $"Account is {account.Status.ToString().ToLowerInvariant()}");
                }

                account.RecordLogin();
                await _accounts.SaveAsync(account, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(account, ct).ConfigureAwait(false);

                _logger.LogInformation("Successful login for account {AccountId}", account.Id);
                return AuthenticateAccountResult.Succeeded(
                    account.LinkedUserId.Value, account.Email.Value, account.Roles);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
