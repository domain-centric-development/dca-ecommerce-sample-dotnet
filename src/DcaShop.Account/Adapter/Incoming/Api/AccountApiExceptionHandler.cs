using DcaShop.Account.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DcaShop.Account.Adapter.Incoming.Api;

/// <summary>
/// Translates what is still <em>thrown</em> towards the account context's routes into HTTP answers.
/// </summary>
/// <remarks>
/// <para>
/// The account's use cases return every refusal a caller answers as a case of their result — a wrong password, an
/// address somebody else holds, a password the model will not accept — and <see cref="AuthResource"/> matches over
/// it. What reaches this handler is what no use case returns: a rule of the model no use case names as a case of its
/// own, and a value a value object refuses. The words are shared with the resource through
/// <see cref="AccountProblem"/>.
/// </para>
/// <para>
/// Scoped to this context's own routes: an adapter answers for its own module only. A handler chain is one
/// pipeline for the whole application, so this one declines everything outside <c>RoutePrefixes</c> and the next
/// context's handler gets its turn — what Spring expresses by scoping an advice to a base package.
/// </para>
/// </remarks>
public sealed class AccountApiExceptionHandler : IExceptionHandler
{
    private static readonly string[] RoutePrefixes = ["/api/auth"];

    private readonly ILogger<AccountApiExceptionHandler> _logger;

    public AccountApiExceptionHandler(ILogger<AccountApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (!IsOwnRoute(httpContext))
        {
            return ValueTask.FromResult(false);
        }

        switch (exception)
        {
            // The account exists but is closed.
            case AccountClosedException closed:
                return AccountProblem.AccountClosed(closed.AccountId).WriteAsync(httpContext, exception);

            // The account's status does not allow signing in; the status stays in the log.
            case AccountNotAccessibleException inaccessible:
                _logger.LogInformation(
                    "Sign-in refused for account {AccountId}: {Status}", inaccessible.AccountId.Value, inaccessible.Status);
                return AccountProblem.AccountNotAccessible().WriteAsync(httpContext, exception);

            // A rule of the model refused the request that no use case returns as a case.
            case DomainException:
                return AccountProblem.BusinessRuleViolated().WriteAsync(httpContext, exception);

            // A value the caller sent is not acceptable to a value object — a malformed address, a blank name.
            // The one mapping that stays imprecise: this is also the type a defect in this application raises,
            // and nothing in the type tells the two apart.
            case ArgumentException:
                _logger.LogDebug("Rejected request value: {Reason}", exception.Message);
                return AccountProblem.UnacceptableValue().WriteAsync(httpContext, exception);

            default:
                return ValueTask.FromResult(false);
        }
    }

    private static bool IsOwnRoute(HttpContext httpContext) =>
        httpContext.Request.Path.Value is { } path
        && RoutePrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}