using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DcaShop.Cart.Adapter.Incoming.Api;

/// <summary>
/// Translates what is still <em>thrown</em> towards the cart's routes into HTTP answers.
/// </summary>
/// <remarks>
/// <para>
/// The cart's use cases return every refusal a caller answers as a case of their result, and
/// <see cref="ShoppingCartResource"/> matches over it. What reaches this handler is what no use case returns: a value
/// that does not parse, and a rule of the model no use case names as a case of its own. The words are shared with the
/// resource through <see cref="CartProblem"/>.
/// </para>
/// <para>
/// Scoped to this context's own routes: an adapter answers for its own module only. A handler chain is one
/// pipeline for the whole application, so this one declines everything outside <c>RoutePrefixes</c> and the next
/// context's handler gets its turn — what Spring expresses by scoping an advice to a base package.
/// </para>
/// </remarks>
public sealed class CartApiExceptionHandler : IExceptionHandler
{
    private static readonly string[] RoutePrefixes = ["/api/carts"];

    private readonly ILogger<CartApiExceptionHandler> _logger;

    public CartApiExceptionHandler(ILogger<CartApiExceptionHandler> logger)
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
            // A rule of the model refused the request that no use case returns as a case — a calculation over
            // amounts in two currencies, say.
            case DomainException:
                return CartProblem.CartRefusesTheChange().WriteAsync(httpContext, exception);

            // A value the caller sent is not acceptable to a value object. The one mapping that stays imprecise:
            // this is also the type a defect in this application raises, and nothing in the type tells the two
            // apart. Moving each such check into request validation removes the ambiguity, one field at a time.
            case ArgumentException:
                _logger.LogDebug("Rejected request value: {Reason}", exception.Message);
                return CartProblem.UnacceptableValue().WriteAsync(httpContext, exception);

            default:
                return ValueTask.FromResult(false);
        }
    }

    private static bool IsOwnRoute(HttpContext httpContext) =>
        httpContext.Request.Path.Value is { } path
        && RoutePrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}