using System.IO;
using Xunit;

namespace DcaShop.UnitTests.Specification;

/// <summary>
/// Marks a test that drives the shared sample specification. The vectors are copied next to the test assembly only
/// when the build was given <c>-p:SpecificationPath=&lt;checkout&gt;</c>; without them the test is skipped, not failed.
/// </summary>
public sealed class SpecificationFactAttribute : FactAttribute
{
    public SpecificationFactAttribute()
    {
        if (!SpecificationVectors.Present) Skip = SpecificationVectors.SkipReason;
    }
}

/// <inheritdoc cref="SpecificationFactAttribute"/>
public sealed class SpecificationTheoryAttribute : TheoryAttribute
{
    public SpecificationTheoryAttribute()
    {
        if (!SpecificationVectors.Present) Skip = SpecificationVectors.SkipReason;
    }
}

internal static class SpecificationVectors
{
    public static readonly string Root = Path.Combine(System.AppContext.BaseDirectory, "specification");
    public static bool Present => Directory.Exists(Path.Combine(Root, "vectors"));
    public const string SkipReason = "shared specification not supplied (-p:SpecificationPath=../dca-sample-specification)";
}
