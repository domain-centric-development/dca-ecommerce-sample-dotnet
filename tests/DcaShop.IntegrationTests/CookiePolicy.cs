namespace DcaShop.IntegrationTests;

/// <summary>One <c>Set-Cookie</c> header, read the way a browser reads it.</summary>
internal sealed record CookiePolicy(string Name, string? SameSite, bool Secure, bool HttpOnly)
{
    internal static CookiePolicy? Named(IEnumerable<string> setCookieHeaders, string name) =>
        Matching(setCookieHeaders, header => header.StartsWith(name + "=", StringComparison.Ordinal));

    /// <summary>The antiforgery cookie carries a generated suffix, so it is found by its prefix.</summary>
    internal static CookiePolicy? NamedByPrefix(IEnumerable<string> setCookieHeaders, string prefix) =>
        Matching(setCookieHeaders, header => header.StartsWith(prefix, StringComparison.Ordinal));

    private static CookiePolicy? Matching(IEnumerable<string> setCookieHeaders, Func<string, bool> predicate)
    {
        var header = setCookieHeaders.FirstOrDefault(predicate);
        return header is null ? null : Parse(header);
    }

    private static CookiePolicy Parse(string header)
    {
        var attributes = header.Split(';');
        string? sameSite = null;
        var secure = false;
        var httpOnly = false;
        foreach (var attribute in attributes)
        {
            var trimmed = attribute.Trim();
            if (trimmed.StartsWith("samesite=", StringComparison.OrdinalIgnoreCase))
            {
                sameSite = trimmed["samesite=".Length..];
            }
            else if (string.Equals(trimmed, "secure", StringComparison.OrdinalIgnoreCase))
            {
                secure = true;
            }
            else if (string.Equals(trimmed, "httponly", StringComparison.OrdinalIgnoreCase))
            {
                httpOnly = true;
            }
        }

        return new CookiePolicy(attributes[0].Split('=', 2)[0], sameSite, secure, httpOnly);
    }
}
