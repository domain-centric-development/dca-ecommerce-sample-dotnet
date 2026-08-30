using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Pricing.Domain.Model;

/// <summary>Identity of a price record.</summary>
public readonly record struct PriceId(Guid Value) : IId
{
    public static PriceId Generate() => new(Guid.NewGuid());

    public static PriceId Of(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
