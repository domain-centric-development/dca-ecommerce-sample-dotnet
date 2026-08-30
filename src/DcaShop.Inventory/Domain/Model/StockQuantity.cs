using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Inventory.Domain.Model;

/// <summary>A never-negative number of units.</summary>
public readonly record struct StockQuantity : IValue
{
    public StockQuantity(int value)
    {
        if (value < 0)
        {
            throw new ArgumentException("Stock quantity cannot be negative", nameof(value));
        }

        Value = value;
    }

    public int Value { get; }

    public static StockQuantity Of(int value) => new(value);

    public override string ToString() => Value.ToString();
}
