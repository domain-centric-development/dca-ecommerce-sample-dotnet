using DcaShop.Product.Application.CreateProduct;
using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DcaShop.Product.Adapter.Incoming.Api;

/// <summary>
/// Translates the catalog's failures into HTTP answers — the one place in this context that knows the protocol.
/// </summary>
/// <remarks>
/// <para>
/// The use cases raise types, not status codes: <see cref="DuplicateSkuException"/> says the catalog already
/// carries the stock keeping unit, and nothing in the application or the domain layer decides what a caller is
/// told about it. That decision lives here, so the same use case can serve this REST exposure and the tool
/// provider next to it with different answers.
/// </para>
/// <para>
/// Scoped to this context's own routes: an adapter answers for its own module only. A handler chain is one
/// pipeline for the whole application, so this one declines everything outside <c>RoutePrefixes</c> and the next
/// context's handler gets its turn — what Spring expresses by scoping an advice to a base package.
/// </para>
/// </remarks>
public sealed class ProductApiExceptionHandler : IExceptionHandler
{
    /// <summary>The routes this context exposes: its REST resource and its MCP tools.</summary>
    private static readonly string[] RoutePrefixes = ["/api/products", "/mcp"];

    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ProductApiExceptionHandler> _logger;

    public ProductApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ProductApiExceptionHandler> logger)
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
            // The catalog already carries that stock keeping unit — a conflict with existing state.
            case DuplicateSkuException:
                return ProblemAsync(httpContext, exception, StatusCodes.Status409Conflict, "Stock keeping unit already in use");

            // Any other failure the use case reports. The request was understood and refused for a stated reason,
            // which is what 422 means; a use-case failure that deserves its own status gets its own arm above.
            case UseCaseException:
                return ProblemAsync(httpContext, exception, StatusCodes.Status422UnprocessableEntity, "Request cannot be served");

            // A rule of the model refused the request.
            case DomainException:
                return ProblemAsync(httpContext, exception, StatusCodes.Status422UnprocessableEntity, "Business rule violated");

            // A value the caller sent is not acceptable to a value object — a malformed stock keeping unit, a
            // blank name. This is the one mapping that stays imprecise: the same exception type is what the
            // runtime raises for a defect in this application, and nothing in the type tells the two apart.
            // Moving each such check into request validation is what removes the ambiguity, one field at a time.
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

    private async ValueTask<bool> ProblemAsync(HttpContext httpContext, Exception exception, int status, string title)
    {
        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = exception.Message },
        }).ConfigureAwait(false);
    }
}
