using System.Net;
using System.Text.RegularExpressions;

namespace DcaShop.IntegrationTests;

/// <summary>A form posted the way the shop's own pages post it: what is typed, plus what the rendered page carries.</summary>
internal static class Forms
{
    /// <summary>Exactly these fields, and nothing the page carries — no antiforgery token either.</summary>
    public static FormUrlEncodedContent Fields(params (string Name, string Value)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Name, f.Value)));

    /// <summary>The fields plus the antiforgery token the rendered page carries; a page without one fails the test.</summary>
    public static FormUrlEncodedContent Submission(string renderedPage, params (string Name, string Value)[] fields)
    {
        var token = Regex
            .Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""")
            .Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token), "the page carries an antiforgery token");
        return Fields([.. fields, ("__RequestVerificationToken", token)]);
    }

    /// <summary>Every hidden input of the rendered <paramref name="form"/>, the token among them, plus what is typed.</summary>
    public static FormUrlEncodedContent AsRendered(string form, params (string Name, string Value)[] typed)
    {
        var hidden = Regex.Matches(form, @"<input\b[^>]*type=""hidden""[^>]*>")
            .Select(input => (
                Name: Regex.Match(input.Value, @"name=""([^""]+)""").Groups[1].Value,
                Value: WebUtility.HtmlDecode(Regex.Match(input.Value, @"value=""([^""]*)""").Groups[1].Value)));

        return Fields([.. hidden, .. typed]);
    }
}