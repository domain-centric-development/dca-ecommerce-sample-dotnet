using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DcaShop.Cart.Adapter.Incoming.Api;

/// <summary>
/// Translates the cart's failures into HTTP answers — the one place in this context that knows the protocol.
/// </summary>
/// <remarks>
/// <para>
/// Each outcome has a type, so the mapping is a table rather than a chain of message tests. Note that the status
/// does not follow the base type: a position the cart does not hold is a rule of the model and still answers
/// <c>404</c>, because what the caller has to do about it is look for something that exists. Deciding that is
/// this adapter's job and nothing further in.
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

    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<CartApiExceptionHandler> _logger;

    public CartApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<CartApiExceptionHandler> logger)
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
            // No cart of this customer under that identity.
            case CartNotFoundException notFound:
                return ProblemAsync(
                    httpContext, exception, StatusCodes.Status404NotFound, "Cart not found", $"There is no cart {notFound.CartId.Value}.");

            // The assortment carries no article for that product.
            case ArticleNotAvailableException notAvailable:
                return ProblemAsync(
                    httpContext, exception, StatusCodes.Status404NotFound, "Article not available", $"There is no article for product {notAvailable.ProductId.Value}.");

            // The cart does not hold the position the caller named.
            case CartItemNotFoundException notInCart:
                return ProblemAsync(
                    httpContext, exception, StatusCodes.Status404NotFound, "Position not in cart", $"The cart holds no position for {notInCart.Reference}.");

            // Not enough of the article to promise the requested quantity.
            case InsufficientArticleStockException insufficient:
                return ProblemAsync(
                    httpContext, exception, StatusCodes.Status409Conflict, "Not enough stock",
                    $"Requested {insufficient.Requested} of product {insufficient.ProductId.Value}, more than is in stock.");

            // The customer already shops in another cart.
            case ActiveCartAlreadyExistsException alreadyActive:
                return ProblemAsync(
                    httpContext, exception, StatusCodes.Status409Conflict, "Active cart already exists", $"Customer {alreadyActive.CustomerId.Value} already has an active cart.");

            // Any other failure the use case reports.
            case UseCaseException:
                return ProblemAsync(httpContext, exception, StatusCodes.Status422UnprocessableEntity, "Request cannot be served");

            // A rule of the model refused the request — a cart that is no longer active, a completion that
            // already happened.
            case DomainException:
                return ProblemAsync(httpContext, exception, StatusCodes.Status409Conflict, "Cart refuses the change");

            // A value the caller sent is not acceptable to a value object. The one mapping that stays imprecise:
            // this is also the type a defect in this application raises, and nothing in the type tells the two
            // apart. Moving each such check into request validation removes the ambiguity, one field at a time.
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