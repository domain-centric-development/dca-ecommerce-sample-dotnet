using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Pricing.Adapter.Incoming.Api;

/// <summary>
/// One answer of the price route to a refusal: a problem document (RFC 9457) with its status and title. The words
/// live here once, for both translation sites — <see cref="ProductPriceResource"/>, which answers the cases of the
/// use case's result, and <see cref="PricingApiExceptionHandler"/>, which answers what is still thrown.
/// </summary>
/// <remarks>
/// Two ways to write one document, so that a refusal reads the same byte for byte whichever site answers it. The
/// exception handler writes through the application's <see cref="IProblemDetailsService"/>; from an action that
/// service would pick MVC's writer, whose formatter adds a charset to the content type — so an action's answer is
/// made by MVC's <see cref="ProblemDetailsFactory"/> and written as <c>application/problem+json</c> by this class.
/// </remarks>
internal sealed class PricingProblem : ActionResult
{
    private PricingProblem(int status, string title)
    {
        Status = status;
        Title = title;
    }

    public int Status { get; }

    public string Title { get; }

    /// <summary>Pricing holds no price for the product the address names.</summary>
    public static PricingProblem ProductPriceNotFound() => new(StatusCodes.Status404NotFound, "Not Found");

    /// <summary>A value the model refuses after normalising it — a price above the highest, one that rounds to zero.</summary>
    public static PricingProblem UnacceptableValue() =>
        new(StatusCodes.Status400BadRequest, ProductPriceResource.UnacceptableValue);

    /// <summary>The answer of an action.</summary>
    public override async Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var httpContext = context.HttpContext;
        var problem = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
            .CreateProblemDetails(httpContext, Status, Title);
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
            ProblemDetails = new ProblemDetails { Status = Status, Title = Title },
        }).ConfigureAwait(false);
    }
}