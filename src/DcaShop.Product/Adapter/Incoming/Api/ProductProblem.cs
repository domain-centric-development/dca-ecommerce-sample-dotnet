using System.Text.Json;

using DcaShop.Product.Domain.Model;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Product.Adapter.Incoming.Api;

/// <summary>
/// One answer of the catalog API to a refusal: a problem document (RFC 9457) with its status, title and detail. The
/// words live here once, for both translation sites — <see cref="ProductResource"/>, which answers the cases of a use
/// case's result, and <see cref="ProductApiExceptionHandler"/>, which answers what is still thrown.
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
internal sealed class ProductProblem : ActionResult
{
    private ProductProblem(int status, string title, string? detail = null)
    {
        Status = status;
        Title = title;
        Detail = detail;
    }

    public int Status { get; }

    public string Title { get; }

    public string? Detail { get; }

    /// <summary>The catalog already carries that stock keeping unit — a conflict with existing state.</summary>
    public static ProductProblem DuplicateSku(Sku sku) =>
        new(StatusCodes.Status409Conflict, "Stock keeping unit already in use", $"A product with SKU {sku.Value} already exists.");

    /// <summary>A rule of the model refused the request.</summary>
    public static ProductProblem BusinessRuleViolated() =>
        new(StatusCodes.Status422UnprocessableEntity, "Business rule violated");

    /// <summary>A value the caller sent is not acceptable to a value object.</summary>
    public static ProductProblem UnacceptableValue() =>
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
}