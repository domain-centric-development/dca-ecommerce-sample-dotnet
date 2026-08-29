using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

public readonly record struct CartItemId(Guid Value) : IId
{
    public static CartItemId Generate() => new(Guid.NewGuid());

    public static CartItemId Of(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
