using DcaShop.Account.Adapter.Incoming.Security;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DcaShop.Web;

/// <summary>
/// Validates the antiforgery token on every state-changing request, and skips exactly the paths that no cookie
/// can authenticate.
/// </summary>
/// <remarks>
/// The exemption is sound only because the Bearer authentication scheme selected for those paths neither reads nor
/// writes cookies: a cross-site form post to <c>/api/**</c> arrives as an anonymous stranger, so there is nothing
/// to forge. Both halves are stated in ADR-007, and the path list lives in <see cref="TokenOnlyPaths"/> — this
/// filter asks it rather than keeping a second copy that could drift.
/// </remarks>
internal sealed class TokenOnlyAwareAntiforgeryFilter : IAsyncAuthorizationFilter
{
    private static readonly string[] SafeMethods = ["GET", "HEAD", "OPTIONS", "TRACE"];

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.HttpContext.Request;
        if (SafeMethods.Contains(request.Method, StringComparer.OrdinalIgnoreCase)
            || TokenOnlyPaths.IsTokenOnlyEndpoint(request.Path))
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
            // A post that did not come from the shop's own page is refused as forbidden, as the Java sample's CSRF
            // protection refuses it -- not as the framework's default bad request (ADR-005).
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }
}