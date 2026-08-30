using DcaShop.Account.Application.Shared;
using DcaShop.Account.Domain.Gateway;
using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Account.Application.RegisterAccount;

/// <summary>
/// Registers a new account under the visitor's current identity. The <see cref="UserId"/> is deliberately kept:
/// a guest who registers mid-checkout keeps cart and checkout session.
/// </summary>
public sealed class RegisterAccountUseCase : IRegisterAccountInputPort
{
    private readonly IAccountRepository _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public RegisterAccountUseCase(
        IAccountRepository accounts,
        IPasswordHasher passwordHasher,
        IDomainEventPublisher events,
        ITransactionBoundary transactionBoundary)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public async Task<RegisterAccountResult> ExecuteAsync(
        RegisterAccountCommand command, CancellationToken cancellationToken = default)
    {
        var email = Email.Of(command.Email);
        var currentUserId = UserId.Of(command.CurrentUserId);
        var owner = Owner.Of(command.FirstName, command.LastName, command.DateOfBirth);

        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                if (await _accounts.ExistsByEmailAsync(email, ct).ConfigureAwait(false))
                {
                    throw new ArgumentException($"Email is already registered: {email.Value}", nameof(command));
                }

                if (await _accounts.FindByLinkedUserIdAsync(currentUserId, ct).ConfigureAwait(false) is not null)
                {
                    throw new InvalidOperationException("User already has an account");
                }

                var account = Domain.Model.Account.Register(
                    email, owner, command.Password, currentUserId, _passwordHasher);

                await _accounts.SaveAsync(account, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(account, ct).ConfigureAwait(false);

                return new RegisterAccountResult(
                    account.Id.ToString(), account.LinkedUserId.Value, account.Email.Value, account.Roles);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
