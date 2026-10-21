using System.Text.RegularExpressions;

namespace DcaShop.IntegrationTests;

/// <summary>
/// What the review's "Place Order" form posts besides the fields a test names: the fingerprint of the order the review
/// showed, as a browser sends it. A page without one adds nothing.
/// </summary>
internal static class ReviewForm
{
    public static IEnumerable<(string Key, string Value)> Fingerprint(string renderedPage) =>
        Regex.Matches(renderedPage, @"name=""fingerprint"" value=""([^""]*)""")
            .Take(1)
            .Select(match => ("fingerprint", match.Groups[1].Value));
}