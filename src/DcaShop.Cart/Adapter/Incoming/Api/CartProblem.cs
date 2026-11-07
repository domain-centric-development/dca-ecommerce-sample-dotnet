using System.Text.Json;

using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Cart.Adapter.Incoming.Api;

/// <summary>
/// One answer of the cart API to a refusal: a problem document (RFC 9457) with its status, title and detail. The words
/// live here once, for both translation sites — <see cref="ShoppingCartResource"/>, which answers the cases of a use
/// case's result, and <see cref="CartApiExceptionHandler"/>, which answers what is still thrown.
/// </summary>
/// <remarks>
/// <para>
/// The detail is worded here, from the refusal's values; a catch-all answer carries its title alone.
/// </para>
/// <para>
/// Two ways to write one document, so that a refusal reads the same byte for byte whichever site answers it. The
/// exception handler writes through the application's <see cref="IProblemDetailsService"/>, outside any endpoint. From
/// an action the same service picks MVC's writer, whose formatter adds a charset to the content type — so an action's
/// answer is made by MVC's <see cref="ProblemDetailsFactory"/> (type link and trace id as before) and written as
/// <c>application/problem+json</c> by this class itself.
/// </para>
/// </remarks>
internal sealed class CartProblem : ActionResult
{
    private CartProblem(int status, string title, string? detail = null)
    {
        Status = status;
        Title = title;
        Detail = detail;
    }

    public int Status { get; }

    public string Title { get; }

    public string? Detail { get; }

    /// <summary>No cart of this customer under that identity.</summary>
    public static CartProblem CartNotFound(CartId cartId) =>
        new(StatusCodes.Status404NotFound, "Cart not found", $"There is no cart {cartId.Value}.");

    /// <summary>
    /// The assortment carries no article for the product the body names. The request is well-formed but cannot be
    /// served, so 422; a 404 stays for what the address names.
    /// </summary>
    public static CartProblem ArticleNotAvailable(ProductId productId) =>
        new(StatusCodes.Status422UnprocessableEntity, "Article not available", $"There is no article for product {productId.Value}.");

    /// <summary>
    /// The cart does not hold the position the caller named. A rule of the model, and still a 404: what the caller
    /// has to do about it is look for something that exists.
    /// </summary>
    public static CartProblem PositionNotInCart(Guid position) =>
        new(StatusCodes.Status404NotFound, "Position not in cart", $"The cart holds no position for {position}.");

    /// <summary>Not enough of the article to promise the requested quantity.</summary>
    public static CartProblem NotEnoughStock(ProductId productId, int requested) =>
        new(StatusCodes.Status409Conflict, "Not enough stock", $"Requested {requested} of product {productId.Value}, more than is in stock.");

    /// <summary>The customer already shops in another cart.</summary>
    public static CartProblem ActiveCartAlreadyExists(CustomerId customerId) =>
        new(StatusCodes.Status409Conflict, "Active cart already exists", $"Customer {customerId.Value} already has an active cart.");

    /// <summary>A rule of the model refused the change — a cart that is no longer active, among others.</summary>
    public static CartProblem CartRefusesTheChange() =>
        new(StatusCodes.Status409Conflict, "Cart refuses the change");

    /// <summary>A value the caller sent is not acceptable.</summary>
    public static CartProblem UnacceptableValue() =>
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