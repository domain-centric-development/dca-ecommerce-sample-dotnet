using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DcaShop.Pricing.Adapter.Incoming.Api;

/// <summary>
/// Translates what is still <em>thrown</em> towards Pricing's route into HTTP answers.
/// </summary>
/// <remarks>
/// <para>
/// The use case returns its refusal as a case of its result, and <see cref="ProductPriceResource"/> matches over it.
/// What reaches this handler is a value the model refuses; the words are shared through <see cref="PricingProblem"/>.
/// </para>
/// <para>
/// Scoped to this context's own route, the price below a catalogue product (<c>/api/products/{id}/price</c>); the
/// catalog's handler declines that route although it shares the prefix, and this one declines everything else.
/// </para>
/// </remarks>
public sealed class PricingApiExceptionHandler : IExceptionHandler
{
    private const string RoutePrefix = "/api/products/";
    private const string RouteSuffix = "/price";

    private readonly ILogger<PricingApiExceptionHandler> _logger;

    public PricingApiExceptionHandler(ILogger<PricingApiExceptionHandler> logger)
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
            // A value the model refuses after normalising it — a price above the highest, one that rounds to zero.
            case ArgumentException:
                _logger.LogDebug("Rejected request value: {Reason}", exception.Message);
                return PricingProblem.UnacceptableValue().WriteAsync(httpContext, exception);

            default:
                return ValueTask.FromResult(false);
        }
    }

    private static bool IsOwnRoute(HttpContext httpContext) =>
        httpContext.Request.Path.Value is { } path
        && path.StartsWith(RoutePrefix, StringComparison.OrdinalIgnoreCase)
        && path.TrimEnd('/').EndsWith(RouteSuffix, StringComparison.OrdinalIgnoreCase);
}