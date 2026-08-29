using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>Positive item count of a cart item.</summary>
public readonly record struct Quantity : IValue
{
    public Quantity(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Quantity must be positive");
        }

        Value = value;
    }

    public int Value { get; }

    public static Quantity Of(int value) => new(value);

    public Quantity Increase() => new(Value + 1);

    public Quantity Decrease() => new(Value - 1);

    public Quantity Add(Quantity other) => new(Value + other.Value);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
