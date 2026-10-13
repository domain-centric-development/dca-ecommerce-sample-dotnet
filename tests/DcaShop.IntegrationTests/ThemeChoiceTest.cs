using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The theme choice the shared footer offers, through the real HTTP pipeline, as a browser that never chose a theme
/// receives it.
/// </summary>
public sealed class ThemeChoiceTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ThemeChoiceTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheFooterOffersFiveThemesWithBlueprintSelectedAndShown()
    {
        var client = _factory.CreateClient();

        var catalogue = await client.GetStringAsync("/products");

        var themeChoice = Element(catalogue, "theme-switcher", "</select>");
        Assert.Equal("Theme", Rendered(Match(themeChoice, @"<label[^>]*>(.*?)</label>")));
        var options = Regex.Matches(themeChoice, @"<option\b([^>]*)>(.*?)</option>", RegexOptions.Singleline);
        Assert.Equal(
            new[] { "Blueprint", "Organic", "Sharp & Minimal", "Classic", "Neon + Dark" },
            options.Select(option => Rendered(option.Groups[2].Value)));
        Assert.Equal("Blueprint", Rendered(SelectedOption(options).Groups[2].Value));
        Assert.DoesNotContain("data-theme", Match(catalogue, @"(<html\b[^>]*>)"), StringComparison.Ordinal);
    }

    /// <summary>The option a browser shows selected: the one marked <c>selected</c>, else the first.</summary>
    private static Match SelectedOption(MatchCollection options) =>
        options.FirstOrDefault(option => Regex.IsMatch(option.Groups[1].Value, @"\bselected\b")) ?? options[0];

    /// <summary>The markup from the element carrying <paramref name="dataTest"/> to <paramref name="closingTag"/>.</summary>
    private static string Element(string page, string dataTest, string closingTag)
    {
        var start = page.IndexOf($"data-test=\"{dataTest}\"", StringComparison.Ordinal);
        Assert.True(start >= 0, $"the page has a {dataTest}");
        return page[start..].Split(closingTag)[0];
    }

    private static string Match(string markup, string pattern)
    {
        var match = Regex.Match(markup, pattern, RegexOptions.Singleline);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return match.Groups[1].Value;
    }

    /// <summary>The text a reader sees: tags removed, entities decoded.</summary>
    private static string Rendered(string html) => WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", string.Empty)).Trim();
}