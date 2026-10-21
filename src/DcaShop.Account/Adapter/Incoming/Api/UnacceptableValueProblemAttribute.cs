using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Account.Adapter.Incoming.Api;

/// <summary>
/// Answers a request the validation of its body refused with 400, titled "Unacceptable value", and an <c>errors</c>
/// map from each refused field, spelled as the request spells it, to the reasons.
/// </summary>
/// <remarks>
/// Runs before the framework's own invalid-model answer (order -2000) and takes its place for the action it
/// decorates only; the login keeps the framework's answer.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class UnacceptableValueProblemAttribute : ActionFilterAttribute
{
    public UnacceptableValueProblemAttribute() => Order = -2001;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var problem = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
            .CreateValidationProblemDetails(context.HttpContext, context.ModelState, StatusCodes.Status400BadRequest, "Unacceptable value");
        SpellFieldsAsTheRequest(problem.Errors);

        context.Result = new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }

    /// <summary>Renames each field from the request type's property name to the name the request's JSON carries.</summary>
    private static void SpellFieldsAsTheRequest(IDictionary<string, string[]> errors)
    {
        foreach (var (field, reasons) in errors.ToList())
        {
            errors.Remove(field);
            errors[JsonNamingPolicy.CamelCase.ConvertName(field)] = reasons;
        }
    }
}