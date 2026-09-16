using Microsoft.AspNetCore.Http;

namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>
/// The paths authenticated by an <c>Authorization: Bearer</c> header and nothing else.
/// </summary>
/// <remarks>
/// This list and the antiforgery exemption in the web host are two halves of one decision: these endpoints may
/// skip the antiforgery token <b>only</b> because no browser cookie can authenticate them. The authentication
/// scheme selector and the antiforgery filter both ask this class, so neither keeps a list of its own that could
/// drift (ADR-007).
/// </remarks>
public static class TokenOnlyPaths
{
    public static readonly string[] Prefixes = ["/api/", "/mcp"];

    /// <summary>Whether a path is authenticated by a Bearer token alone.</summary>
    public static bool IsTokenOnlyEndpoint(PathString path) =>
        path.Value is { } value
        && Prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
