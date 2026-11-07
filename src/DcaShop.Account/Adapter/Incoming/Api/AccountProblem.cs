using System.Text.Json;

using DcaShop.Account.Domain.Model;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Account.Adapter.Incoming.Api;

/// <summary>
/// One answer of the account API to a refusal: a problem document (RFC 9457) with its status, title and detail. The
/// words live here once, for both translation sites — <see cref="AuthResource"/>, which answers the cases of a use
/// case's result, and <see cref="AccountApiExceptionHandler"/>, which answers what is still thrown.
/// </summary>
/// <remarks>
/// <para>The detail is worded here, from the refusal's values; a catch-all answer carries its title alone.</para>
/// <para>
/// Two ways to write one document, so that a refusal reads the same byte for byte whichever site answers it. The
/// exception handler writes through the application's <see cref="IProblemDetailsService"/>; from an action that
/// service would pick MVC's writer, whose formatter adds a charset to the content type — so an action's answer is
/// made by MVC's <see cref="ProblemDetailsFactory"/> and written as <c>application/problem+json</c> by this class.
/// </para>
/// </remarks>
internal sealed class AccountProblem : ActionResult
{
    private AccountProblem(int status, string title, string? detail = null)
    {
        Status = status;
        Title = title;
        Detail = detail;
    }

    public int Status { get; }

    public string Title { get; }

    public string? Detail { get; }

    /// <summary>Another account already holds that address.</summary>
    public static AccountProblem EmailAlreadyRegistered(Email email) =>
        new(StatusCodes.Status409Conflict, "Email already registered", $"The email address {email.Value} is already registered.");

    /// <summary>The signed-in user already has an account. The refusal names no attempt, so the sentence is the adapter's own.</summary>
    public static AccountProblem AccountAlreadyExists() =>
        new(StatusCodes.Status409Conflict, "Account already exists", "The signed-in user already has an account.");

    /// <summary>The password does not meet the strength rules. The answer names the broken rule — the rule, never the attempt.</summary>
    public static AccountProblem PasswordTooWeak(PasswordRule rule) =>
        new(StatusCodes.Status422UnprocessableEntity, "Password too weak", PasswordRuleSentence(rule));

    /// <summary>The account exists but is closed.</summary>
    public static AccountProblem AccountClosed(AccountId accountId) =>
        new(StatusCodes.Status409Conflict, "Account closed", $"Account {accountId.Value} is closed.");

    /// <summary>
    /// The account's status does not allow signing in. The status itself is deliberately not in the answer: it would
    /// tell a stranger which addresses have suspended accounts.
    /// </summary>
    public static AccountProblem AccountNotAccessible() =>
        new(StatusCodes.Status403Forbidden, "Account not accessible", "This account cannot sign in.");

    /// <summary>A rule of the model refused the request.</summary>
    public static AccountProblem BusinessRuleViolated() =>
        new(StatusCodes.Status422UnprocessableEntity, "Business rule violated");

    /// <summary>A value the caller sent is not acceptable to a value object.</summary>
    public static AccountProblem UnacceptableValue() =>
        new(StatusCodes.Status400BadRequest, "Unacceptable value");

    /// <summary>The answer of an action.</summary>
    public override async Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var httpContext = context.HttpContext;
        var problem = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
            .CreateProblemDetails(httpContext, Status, Title, detail: Detail);
        httpContext.Response.StatusCode = Status;
        await httpContext.Response
            .WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json", httpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    /// <summary>The answer of the exception handler to <paramref name="exception"/>.</summary>
    public async ValueTask<bool> WriteAsync(HttpContext httpContext, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        httpContext.Response.StatusCode = Status;
        return await httpContext.RequestServices.GetRequiredService<IProblemDetailsService>().TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = Status, Title = Title, Detail = Detail },
        }).ConfigureAwait(false);
    }

    private static string PasswordRuleSentence(PasswordRule rule) => rule switch
    {
        PasswordRule.TooShort => $"Password must be at least {HashedPassword.MinLength} characters long.",
        PasswordRule.TooLong => $"Password must not be longer than {HashedPassword.MaxByteLength} bytes.",
        PasswordRule.NoUppercaseLetter => "Password must contain an uppercase letter.",
        PasswordRule.NoLowercaseLetter => "Password must contain a lowercase letter.",
        PasswordRule.NoDigit => "Password must contain a digit.",
        _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, null),
    };
}