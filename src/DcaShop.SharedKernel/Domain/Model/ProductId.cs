using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Model;

/// <summary>Identity of a product — the one identifier every context agrees on.</summary>
public readonly record struct ProductId(Guid Value) : IId
{
    public static ProductId Generate() => new(Guid.NewGuid());

    public static ProductId Of(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
