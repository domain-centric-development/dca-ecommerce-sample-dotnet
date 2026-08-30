using DcaShop.Account.Adapter.Outgoing.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DcaShop.Web;

/// <summary>
/// Validates the antiforgery token on every state-changing request, and skips exactly the paths that no cookie
/// can authenticate.
/// </summary>
/// <remarks>
/// The exemption is sound only because <see cref="JwtAuthenticationMiddleware"/> neither reads nor writes cookies
/// on those paths: a cross-site form post to <c>/api/**</c> arrives as an anonymous stranger, so there is nothing
/// to forge. Both halves are stated in ADR-007, and the path list lives in the middleware — this filter asks it
/// rather than keeping a second copy that could drift.
/// </remarks>
internal sealed class TokenOnlyAwareAntiforgeryFilter : IAsyncAuthorizationFilter
{
    private static readonly string[] SafeMethods = ["GET", "HEAD", "OPTIONS", "TRACE"];

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.HttpContext.Request;
        if (SafeMethods.Contains(request.Method, StringComparer.OrdinalIgnoreCase)
            || JwtAuthenticationMiddleware.IsTokenOnlyEndpoint(request.Path))
        {
            return;
        }

        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext).ConfigureAwait(false);
        }
        catch (AntiforgeryValidationException)
        {
            // The same answer the framework's own AutoValidateAntiforgeryTokenAttribute gives: a missing or
            // invalid token is a bad request, not a server fault.
            context.Result = new BadRequestResult();
        }
    }
}
