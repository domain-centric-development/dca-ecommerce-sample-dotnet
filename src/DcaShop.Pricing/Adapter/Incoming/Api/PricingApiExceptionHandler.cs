using DcaShop.Pricing.Application.ChangePrice;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DcaShop.Pricing.Adapter.Incoming.Api;

/// <summary>
/// Translates Pricing's failures into HTTP answers — the one place in this context that knows the protocol.
/// </summary>
/// <remarks>
/// Scoped to this context's own route, the price below a catalogue product (<c>/api/products/{id}/price</c>); the
/// catalog's handler declines that route although it shares the prefix, and this one declines everything else.
/// </remarks>
public sealed class PricingApiExceptionHandler : IExceptionHandler
{
    private const string RoutePrefix = "/api/products/";
    private const string RouteSuffix = "/price";

    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<PricingApiExceptionHandler> _logger;

    public PricingApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<PricingApiExceptionHandler> logger)
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
            // Pricing holds no price for the product the address names.
            case ProductPriceNotFoundException:
                return ProblemAsync(httpContext, exception, StatusCodes.Status404NotFound, "Not Found");

            // A value the model refuses after normalising it — a price above the highest, one that rounds to zero.
            case ArgumentException:
                _logger.LogDebug("Rejected request value: {Reason}", exception.Message);
                return ProblemAsync(httpContext, exception, StatusCodes.Status400BadRequest, ProductPriceResource.UnacceptableValue);

            default:
                return ValueTask.FromResult(false);
        }
    }

    private static bool IsOwnRoute(HttpContext httpContext) =>
        httpContext.Request.Path.Value is { } path
        && path.StartsWith(RoutePrefix, StringComparison.OrdinalIgnoreCase)
        && path.TrimEnd('/').EndsWith(RouteSuffix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Writes the problem document with its title alone — the exception's message is for the log.</summary>
    private async ValueTask<bool> ProblemAsync(HttpContext httpContext, Exception exception, int status, string title)
    {
        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title },
        }).ConfigureAwait(false);
    }
}