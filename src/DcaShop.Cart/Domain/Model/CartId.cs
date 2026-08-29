using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

public readonly record struct CartId(Guid Value) : IId
{
    public static CartId Generate() => new(Guid.NewGuid());

    public static CartId Of(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
