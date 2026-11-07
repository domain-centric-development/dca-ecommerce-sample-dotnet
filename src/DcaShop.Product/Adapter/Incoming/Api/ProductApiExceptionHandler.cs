using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DcaShop.Product.Adapter.Incoming.Api;

/// <summary>
/// Translates what is still <em>thrown</em> towards the catalog's routes into HTTP answers.
/// </summary>
/// <remarks>
/// <para>
/// The catalog's use cases return every refusal a caller answers as a case of their result — a stock keeping unit
/// the catalog already carries, a product that does not exist — and <see cref="ProductResource"/> matches over it.
/// What reaches this handler is what no use case returns: a value a value object refuses, and a rule of the model no
/// use case names as a case of its own. The words are shared with the resource through <see cref="ProductProblem"/>.
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

    private const string PriceRouteSuffix = "/price";

    private readonly ILogger<ProductApiExceptionHandler> _logger;

    public ProductApiExceptionHandler(ILogger<ProductApiExceptionHandler> logger)
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
            // A rule of the model refused the request that no use case returns as a case.
            case DomainException:
                return ProductProblem.BusinessRuleViolated().WriteAsync(httpContext, exception);

            // A value the caller sent is not acceptable to a value object — a malformed stock keeping unit, a
            // blank name. This is the one mapping that stays imprecise: the same exception type is what the
            // runtime raises for a defect in this application, and nothing in the type tells the two apart.
            // Moving each such check into request validation is what removes the ambiguity, one field at a time --
            // except a value refused only after the model normalised it: a price that rounds to zero stays the
            // value object's refusal, because only the value object knows the rounding.
            case ArgumentException:
                _logger.LogDebug("Rejected request value: {Reason}", exception.Message);
                return ProductProblem.UnacceptableValue().WriteAsync(httpContext, exception);

            default:
                return ValueTask.FromResult(false);
        }
    }

    /// <summary>
    /// The price below a product (<c>/api/products/{id}/price</c>) shares the prefix but is Pricing's route, with
    /// Pricing's translation site; declining it keeps one owner per route, whatever order the handlers run in.
    /// </summary>
    private static bool IsOwnRoute(HttpContext httpContext) =>
        httpContext.Request.Path.Value is { } path
        && RoutePrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        && !path.TrimEnd('/').EndsWith(PriceRouteSuffix, StringComparison.OrdinalIgnoreCase);
}