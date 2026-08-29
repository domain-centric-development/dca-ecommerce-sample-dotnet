using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Model;

/// <summary>A price: money that is greater than zero.</summary>
public sealed record Price : IValue
{
    public Price(Money value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.IsZero)
        {
            throw new ArgumentException("Price must be greater than zero", nameof(value));
        }

        Value = value;
    }

    public Money Value { get; }

    public static Price Of(Money money) => new(money);

    public bool IsHigherThan(Price other) => Value.IsGreaterThan(other.Value);

    public Money Multiply(int quantity) => Value.Multiply(quantity);
}
