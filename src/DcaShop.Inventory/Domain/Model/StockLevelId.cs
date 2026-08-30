using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Inventory.Domain.Model;

/// <summary>Identity of a stock level record.</summary>
public readonly record struct StockLevelId(Guid Value) : IId
{
    public static StockLevelId Generate() => new(Guid.NewGuid());

    public static StockLevelId Of(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
