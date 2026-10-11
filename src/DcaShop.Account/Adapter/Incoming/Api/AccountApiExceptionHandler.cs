using DcaShop.Account.Application.RegisterAccount;
using DcaShop.Account.Domain.Model;

using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DcaShop.Account.Adapter.Incoming.Api;

/// <summary>
/// Translates the account context's failures into HTTP answers — the one place in this context that knows the
/// protocol.
/// </summary>
/// <remarks>
/// <para>
/// Authentication itself does not arrive here: a wrong password is an outcome of the authentication use case,
/// returned as a value, and the resource renders it. What arrives are the refusals: an address somebody else
/// holds, a password the model will not accept, an account that is closed.
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

    /// <summary>The failure names no attempt, so the sentence is the adapter's own.</summary>
    private const string AccountAlreadyExistsDetail = "User already has an account";

    /// <summary>
    /// The strength rule the model applies, in this adapter's words: the refusal carries no value, and the attempt
    /// itself is never repeated back.
    /// </summary>
    private static readonly string PasswordTooWeakDetail =
        $"Password must be at least {HashedPassword.MinLength} characters long and contain an uppercase letter, a lowercase letter and a digit";

    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<AccountApiExceptionHandler> _logger;

    public AccountApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<AccountApiExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
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
            // Another account already holds that address.
            case EmailAlreadyRegisteredException taken:
                return ProblemAsync(
                    httpContext, exception, StatusCodes.Status409Conflict, "Email already registered", $"Email is already registered: {taken.Email.Value}");

            // The signed-in user already has an account.
            case AccountAlreadyExistsException:
                return ProblemAsync(httpContext, exception, StatusCodes.Status409Conflict, "Account already exists", AccountAlreadyExistsDetail);

            // The password does not meet the strength rules. The answer states the rule, not the attempt.
            case PasswordTooWeakException:
                return ProblemAsync(httpContext, exception, StatusCodes.Status422UnprocessableEntity, "Password too weak", PasswordTooWeakDetail);

            // The account exists but is closed.
            case AccountClosedException closed:
                return ProblemAsync(
                    httpContext, exception, StatusCodes.Status409Conflict, "Account closed", $"Account {closed.AccountId.Value} is closed");

            // The account's status does not allow signing in. The status itself is deliberately not in the
            // answer: it would tell a stranger which addresses have suspended accounts.
            case AccountNotAccessibleException inaccessible:
                _logger.LogInformation(
                    "Sign-in refused for account {AccountId}: {Status}", inaccessible.AccountId.Value, inaccessible.Status);
                return ProblemAsync(httpContext, exception, StatusCodes.Status403Forbidden, "Account not accessible", "This account cannot sign in");

            // Any other failure the use case reports.
            case UseCaseException:
                return ProblemAsync(httpContext, exception, StatusCodes.Status422UnprocessableEntity, "Request cannot be served");

            // A rule of the model refused the request.
            case DomainException:
                return ProblemAsync(httpContext, exception, StatusCodes.Status422UnprocessableEntity, "Business rule violated");

            // A value the caller sent is not acceptable to a value object — a malformed address, a blank name.
            // The one mapping that stays imprecise: this is also the type a defect in this application raises,
            // and nothing in the type tells the two apart.
            case ArgumentException:
                _logger.LogDebug("Rejected request value: {Reason}", exception.Message);
                return ProblemAsync(httpContext, exception, StatusCodes.Status400BadRequest, "Unacceptable value");

            default:
                return ValueTask.FromResult(false);
        }
    }

    private static bool IsOwnRoute(HttpContext httpContext) =>
        httpContext.Request.Path.Value is { } path
        && RoutePrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Writes the problem document. The detail is worded here, from the failure's values — the exception's message
    /// is for the log, never for the caller; a catch-all arm answers with its title alone.
    /// </summary>
    private async ValueTask<bool> ProblemAsync(
        HttpContext httpContext, Exception exception, int status, string title, string? detail = null)
    {
        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        }).ConfigureAwait(false);
    }
}