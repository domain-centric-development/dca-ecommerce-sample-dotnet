using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Product;

public sealed class ProductSelectionTest
{
    [Fact]
    public void DrawsEightDifferentProductsFromTheCandidates()
    {
        var candidates = Candidates(21);

        var selection = ProductSelection.Draw(candidates, new Random(1));

        Assert.Equal(8, selection.ProductIds.Count);
        Assert.Equal(8, selection.ProductIds.Distinct().Count());
        Assert.All(selection.ProductIds, id => Assert.Contains(id, candidates));
    }

    [Fact]
    public void DrawsEveryCandidateWhenThereAreFewerThanEight()
    {
        var candidates = Candidates(2);

        var selection = ProductSelection.Draw(candidates, new Random(1));

        Assert.Equal(candidates.ToHashSet(), selection.ProductIds.ToHashSet());
        Assert.Equal(2, selection.ProductIds.Count);
    }

    [Fact]
    public void DrawsNothingWithoutCandidates()
    {
        var selection = ProductSelection.Draw(Array.Empty<ProductId>(), new Random(1));

        Assert.Empty(selection.ProductIds);
    }

    [Fact]
    public void DifferentRandomSourcesDrawDifferentSelections()
    {
        var candidates = Candidates(21);

        var selections = Enumerable.Range(1, 20)
            .Select(seed => string.Join(",", ProductSelection.Draw(candidates, new Random(seed)).ProductIds.Select(id => id.Value).Order()))
            .ToHashSet();

        Assert.True(selections.Count > 1, "twenty different random sources all drew the same eight products");
    }

    [Fact]
    public void TheSameRandomSourceDrawsTheSameSelection()
    {
        var candidates = Candidates(21);

        var first = ProductSelection.Draw(candidates, new Random(42));
        var second = ProductSelection.Draw(candidates, new Random(42));

        Assert.Equal(first.ProductIds, second.ProductIds);
    }

    private static IReadOnlyList<ProductId> Candidates(int count) =>
        Enumerable.Range(0, count).Select(_ => ProductId.Generate()).ToList();
}