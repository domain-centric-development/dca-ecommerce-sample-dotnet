using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DcaShop.UnitTests.Specification;

/// <summary>
/// The end-user suite implements the scenarios of this shop's specification (<c>product/spec/**/scenarios.md</c>):
/// every browser test carries a known scenario's title as its display name, no title names two tests, and every
/// scenario whose <c>Test:</c> line names a browser test names one that exists. A scenario's title is its <c>Title:</c>
/// line, else its key in words. A known title is a scenario's of the specification or an acceptance criterion's of a
/// story not yet delivered (<c>product/epics/**/story.md</c>, <c>status:</c> neither <c>delivered</c> nor
/// <c>superseded</c>): its scenario reaches the specification only when the gate delivers the story, and its browser
/// test exists before that. The uniqueness of titles and the <c>Test:</c> lines are checked on the specification alone.
/// Checked against the sources of <c>tests/DcaShop.E2eTests</c>, so it holds without a running shop or a browser.
/// </summary>
public sealed class SharedScenariosTest
{
    private static readonly Regex TestAttribute = new(@"\[(\w+Fact|\w+Theory)\b", RegexOptions.Compiled);
    private static readonly Regex DisplayName = new(@"DisplayName\s*=\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);
    private static readonly Regex ClassDeclaration = new(@"\bclass\s+(\w+)", RegexOptions.Compiled);
    private static readonly Regex HappyPath = new(@"\s+\(happy path\)\s*$", RegexOptions.Compiled);
    private static readonly Regex MethodDeclaration = new(@"\bpublic\s+(?:async\s+)?[\w<>]+\s+(\w+)\s*\(", RegexOptions.Compiled);
    private static readonly Regex BacktickedKey = new(@"`([a-z0-9]+(?:-[a-z0-9]+)+)`", RegexOptions.Compiled);

    /// <summary>The states of a story whose criteria are no title to come: delivered, or replaced by another story.</summary>
    private static readonly HashSet<string> Settled = new(["delivered", "superseded"], StringComparer.OrdinalIgnoreCase);

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
        var known = titles.Concat(StoryTitles()).ToHashSet(StringComparer.Ordinal);

        var bound = new List<string>();
        var tests = 0;
        foreach (var source in BrowserTestSources())
        {
            var text = File.ReadAllText(source);
            tests += TestAttribute.Matches(text).Count;
            bound.AddRange(DisplayName.Matches(text).Select(m => Unescape(m.Groups[1].Value)));
        }
        Assert.True(tests == bound.Count, "every browser test names its scenario with DisplayName");

        Assert.Empty(bound.Where(t => !known.Contains(t) && !TitledAfterTheSharedSpecification.Contains(t)).Order());
        Assert.Empty(bound.GroupBy(t => t).Where(g => g.Count() > 1).Select(g => g.Key).Order());
        Assert.Empty(TitledAfterTheSharedSpecification.Where(t => titles.Contains(t) || !bound.Contains(t)).Order());
    }

    [Fact]
    public void AStoryNotYetDeliveredNamesItsCriteriaAsScenarios()
    {
        string[] story =
        [
            "---", "id: a-story", "status: approved", "---", "", "# A story", "", "## Acceptance criteria", "",
            "### Rule: one rule", "", "#### takes-the-line-out (happy path)", "- Given a cart", "",
            "#### keeps-the-order", "Title: The other lines keep their order", "- Given a cart", "",
            "## Out of scope", "", "#### not-a-criterion",
        ];

        Assert.Equal("approved", Status(story));
        Assert.Equal(["Takes the line out", "The other lines keep their order"], StoryScenariosIn(story).Select(s => s.Title));
    }

    /// <summary>
    /// A scenario that a story not yet delivered changes or removes may name a browser test the story has already
    /// renamed or retired: its <c>Test:</c> lines are rewritten only when the gate delivers the story.
    /// </summary>
    [Fact]
    public void EveryBrowserTestAScenarioNamesExists()
    {
        var browserTests = BrowserTestSources().SelectMany(BrowserTestsIn).ToHashSet(StringComparer.Ordinal);
        var changing = UndeliveredStories().SelectMany(ChangedScenarioKeys).ToHashSet(StringComparer.Ordinal);

        var named = Scenarios().Where(s => !changing.Contains(s.Key)).SelectMany(s => s.Tests)
            .Where(test => test.StartsWith("DcaShop.E2eTests.", StringComparison.Ordinal));

        Assert.Empty(named.Where(test => !browserTests.Contains(test)).Order());
    }

    [Fact]
    public void AStoryNotYetDeliveredChangesItsCriteriaAndRemovesTheScenariosItsChangedExpectationsName()
    {
        string[] story =
        [
            "---", "id: a-story", "status: approved", "---", "", "# A story", "", "## Acceptance criteria", "",
            "### Rule: one rule", "", "#### takes-the-line-out (happy path)", "- Given a cart", "",
            "## Changed expectations", "", "- The rule goes with its scenario `keeps-the-order`.", "",
            "## Notes", "", "- `not-a-removal` is only mentioned.",
        ];

        Assert.Equal(["takes-the-line-out", "keeps-the-order"], ChangedScenarioKeys(story));
    }

    /// <summary>
    /// Each scenario's key and title with the tests its <c>Test:</c> lines name, read from every <c>scenarios.md</c>
    /// below <c>product/spec</c>. Two contexts may hold a scenario of the same title; only a browser test's title has
    /// to be unique.
    /// </summary>
    private static List<(string Key, string Title, List<string> Tests)> Scenarios() =>
        Directory.GetFiles(Path.Combine(RepositoryRoot(), "product", "spec"), "scenarios.md", SearchOption.AllDirectories)
            .SelectMany(file => ScenariosIn(File.ReadAllLines(file)))
            .ToList();

    /// <summary>
    /// The titles of the acceptance criteria of every story below <c>product/epics</c> that is not delivered yet:
    /// the scenarios the gate will add to the specification when it delivers the story.
    /// </summary>
    private static IEnumerable<string> StoryTitles() =>
        UndeliveredStories().SelectMany(lines => StoryScenariosIn(lines).Select(s => s.Title));

    private static IEnumerable<string[]> UndeliveredStories() =>
        Directory.GetFiles(Path.Combine(RepositoryRoot(), "product", "epics"), "story.md", SearchOption.AllDirectories)
            .Select(File.ReadAllLines)
            .Where(lines => !Settled.Contains(Status(lines)));

    /// <summary>
    /// The scenario keys a story changes or removes: its criteria's keys, and every key in backticks under its
    /// <c>## Changed expectations</c>.
    /// </summary>
    private static List<string> ChangedScenarioKeys(IReadOnlyList<string> lines) =>
        StoryScenariosIn(lines).Select(s => s.Key)
            .Concat(Section(lines, "## Changed expectations")
                .SelectMany(line => BacktickedKey.Matches(line).Select(m => m.Groups[1].Value)))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static IEnumerable<string> Section(IEnumerable<string> lines, string heading) =>
        lines
            .SkipWhile(line => !line.StartsWith(heading, StringComparison.Ordinal))
            .Skip(1)
            .TakeWhile(line => !line.StartsWith("## ", StringComparison.Ordinal));

    /// <summary>A story's <c>status:</c> from its front matter, empty without one.</summary>
    private static string Status(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0 || lines[0].Trim() != "---")
        {
            return "";
        }

        foreach (var line in lines.Skip(1).TakeWhile(l => l.Trim() != "---"))
        {
            if (line.StartsWith("status:", StringComparison.Ordinal))
            {
                return line[7..].Trim();
            }
        }

        return "";
    }

    /// <summary>
    /// The scenarios of a story's <c>## Acceptance criteria</c> section, read like a <c>scenarios.md</c>; a key's
    /// trailing <c>(happy path)</c> marker is not part of it.
    /// </summary>
    private static List<(string Key, string Title, List<string> Tests)> StoryScenariosIn(IEnumerable<string> lines) =>
        ScenariosIn(Section(lines, "## Acceptance criteria")
            .Select(line => line.StartsWith("#### ", StringComparison.Ordinal)
                ? HappyPath.Replace(line, "")
                : line));

    /// <summary>
    /// The scenarios of one document: a <c>#### key</c> heading, an optional Title line, the Test lines; a
    /// <c>###</c> heading ends a scenario.
    /// </summary>
    private static List<(string Key, string Title, List<string> Tests)> ScenariosIn(IEnumerable<string> lines)
    {
        var scenarios = new List<(string Key, string Title, List<string> Tests)>();
        string? key = null;
        string? title = null;
        var tests = new List<string>();
        foreach (var line in lines.Append("#### "))
        {
            if (line.StartsWith("#### ", StringComparison.Ordinal) || line.StartsWith("### ", StringComparison.Ordinal))
            {
                if (key is not null)
                {
                    scenarios.Add((key, title ?? InWords(key), tests));
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