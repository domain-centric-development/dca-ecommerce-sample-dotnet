using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DcaShop.UnitTests.Specification;

/// <summary>
/// The end-user suite implements the scenarios of this shop's specification (<c>product/spec/**/scenarios.md</c>):
/// every browser test carries a scenario's title as its display name, no title names two tests, and every scenario
/// whose <c>Test:</c> line names a browser test names one that exists. A scenario's title is its <c>Title:</c> line,
/// else its key in words. Checked against the sources of <c>tests/DcaShop.E2eTests</c>, so it holds without a running
/// shop or a browser.
/// </summary>
public sealed class SharedScenariosTest
{
    private static readonly Regex TestAttribute = new(@"\[(\w+Fact|\w+Theory)\b", RegexOptions.Compiled);
    private static readonly Regex DisplayName = new(@"DisplayName\s*=\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);
    private static readonly Regex ClassDeclaration = new(@"\bclass\s+(\w+)", RegexOptions.Compiled);
    private static readonly Regex MethodDeclaration = new(@"\bpublic\s+(?:async\s+)?[\w<>]+\s+(\w+)\s*\(", RegexOptions.Compiled);

    /// <summary>
    /// Browser tests still titled after the shared specification's scenarios, which this shop's specification describes
    /// under other titles or not at all. The list only shrinks: a title that becomes a scenario's, or that no browser
    /// test carries any more, fails until it is taken off.
    /// </summary>
    private static readonly HashSet<string> TitledAfterTheSharedSpecification = [];

    [Fact]
    public void EveryBrowserTestIsAScenarioOfThisShopsSpecification()
    {
        var titles = Scenarios().Select(s => s.Title).ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(titles);

        var bound = new List<string>();
        var tests = 0;
        foreach (var source in BrowserTestSources())
        {
            var text = File.ReadAllText(source);
            tests += TestAttribute.Matches(text).Count;
            bound.AddRange(DisplayName.Matches(text).Select(m => Unescape(m.Groups[1].Value)));
        }
        Assert.True(tests == bound.Count, "every browser test names its scenario with DisplayName");

        Assert.Empty(bound.Where(t => !titles.Contains(t) && !TitledAfterTheSharedSpecification.Contains(t)).Order());
        Assert.Empty(bound.GroupBy(t => t).Where(g => g.Count() > 1).Select(g => g.Key).Order());
        Assert.Empty(TitledAfterTheSharedSpecification.Where(t => titles.Contains(t) || !bound.Contains(t)).Order());
    }

    [Fact]
    public void EveryBrowserTestAScenarioNamesExists()
    {
        var browserTests = BrowserTestSources().SelectMany(BrowserTestsIn).ToHashSet(StringComparer.Ordinal);

        var named = Scenarios().SelectMany(s => s.Tests)
            .Where(test => test.StartsWith("DcaShop.E2eTests.", StringComparison.Ordinal));

        Assert.Empty(named.Where(test => !browserTests.Contains(test)).Order());
    }

    /// <summary>
    /// Each scenario's title with the tests its <c>Test:</c> lines name, read from every <c>scenarios.md</c> below
    /// <c>product/spec</c>: a <c>#### key</c> heading, an optional Title line, the Test lines. Two contexts may hold a
    /// scenario of the same title; only a browser test's title has to be unique.
    /// </summary>
    private static List<(string Title, List<string> Tests)> Scenarios()
    {
        var scenarios = new List<(string Title, List<string> Tests)>();
        foreach (var file in Directory.GetFiles(Path.Combine(RepositoryRoot(), "product", "spec"), "scenarios.md", SearchOption.AllDirectories))
        {
            string? key = null;
            string? title = null;
            var tests = new List<string>();
            foreach (var line in File.ReadAllLines(file).Append("#### "))
            {
                if (line.StartsWith("#### ", StringComparison.Ordinal) || line.StartsWith("### ", StringComparison.Ordinal))
                {
                    if (key is not null)
                    {
                        scenarios.Add((title ?? InWords(key), tests));
                    }

                    key = line.StartsWith("#### ", StringComparison.Ordinal) && line.Length > 5 ? line[5..].Trim() : null;
                    title = null;
                    tests = [];
                }
                else if (key is not null && line.StartsWith("Title:", StringComparison.Ordinal))
                {
                    title = line[6..].Trim();
                }
                else if (key is not null && line.StartsWith("Test:", StringComparison.Ordinal))
                {
                    tests.AddRange(line[5..].Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries));
                }
            }
        }

        return scenarios;
    }

    /// <summary>A scenario's key in words, the title it has without a Title line: "shows-empty-state" is "Shows empty state".</summary>
    private static string InWords(string key) => char.ToUpperInvariant(key[0]) + key[1..].Replace('-', ' ');

    /// <summary>The browser tests of one source as <c>Namespace.Class#Method</c>, the way a Test line names them.</summary>
    private static IEnumerable<string> BrowserTestsIn(string source)
    {
        var text = File.ReadAllText(source);
        foreach (Match attribute in TestAttribute.Matches(text))
        {
            var className = ClassDeclaration.Matches(text[..attribute.Index]).Last().Groups[1].Value;
            var method = MethodDeclaration.Match(text, attribute.Index).Groups[1].Value;
            yield return $"DcaShop.E2eTests.{className}#{method}";
        }
    }

    /// <summary>
    /// The display name as the test runner sees it: the C# literal read from source with <c>\"</c> turned into
    /// <c>"</c> and <c>\\</c> into <c>\</c>, so a scenario title with quotes matches its test.
    /// </summary>
    private static string Unescape(string literal) =>
        Regex.Replace(literal, @"\\([""\\])", "$1");

    private static string[] BrowserTestSources() =>
        Directory.GetFiles(Path.Combine(RepositoryRoot(), "tests", "DcaShop.E2eTests"), "*E2eTest.cs");

    private static string RepositoryRoot([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", ".."));
}