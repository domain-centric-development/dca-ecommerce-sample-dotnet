using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DcaShop.UnitTests.Specification;

/// <summary>
/// The end-user suite implements the shared scenarios (<c>scenarios.md</c> in the specification) and nothing else:
/// every scenario title is the display name of exactly one browser test, and every browser test carries a scenario
/// title. Checked against the sources of <c>tests/DcaShop.E2eTests</c>, so it holds without a running shop or a
/// browser. Runs only when the build was given the local specification checkout (<c>-p:SpecificationPath=…</c>).
/// </summary>
public sealed class SharedScenariosTest
{
    private static readonly Regex TestAttribute = new(@"\[(E2eFact|E2eTheory|EmbeddedModeFact)\b", RegexOptions.Compiled);
    private static readonly Regex DisplayName = new(@"DisplayName\s*=\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);

    [SpecificationFact]
    public void EveryScenarioIsOneBrowserTestAndEveryBrowserTestIsAScenario()
    {
        var scenarios = ScenarioTitles();
        Assert.NotEmpty(scenarios);

        var bound = new List<string>();
        var tests = 0;
        foreach (var source in Directory.GetFiles(E2eSources(), "*E2eTest.cs"))
        {
            var text = File.ReadAllText(source);
            tests += TestAttribute.Matches(text).Count;
            bound.AddRange(DisplayName.Matches(text).Select(m => m.Groups[1].Value));
        }
        Assert.True(tests == bound.Count, "every browser test names its scenario with DisplayName");

        Assert.Empty(scenarios.Keys.Except(bound).Order());
        Assert.Empty(bound.Except(scenarios.Keys).Order());
        Assert.Empty(bound.GroupBy(t => t).Where(g => g.Count() > 1).Select(g => g.Key).Order());
    }

    /// <summary>Scenario title by id, read from <c>scenarios.md</c>: a <c>## id</c> heading, then a Title line.</summary>
    private static Dictionary<string, string> ScenarioTitles()
    {
        var titles = new Dictionary<string, string>(StringComparer.Ordinal);
        string? id = null;
        foreach (var line in File.ReadAllLines(Path.Combine(SpecificationVectors.Root, "scenarios.md")))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal)) id = line[3..].Trim();
            else if (line.StartsWith("Title:", StringComparison.Ordinal) && id != null)
            {
                Assert.True(titles.TryAdd(line[6..].Trim(), id), "duplicate scenario title: " + line);
                id = null;
            }
        }
        return titles;
    }

    private static string E2eSources([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "DcaShop.E2eTests"));
}