using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>Customer-facing display name, at most 255 characters.</summary>
public sealed record ProductName : IValue
{
    public ProductName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Product name cannot be blank", nameof(value));
        }

        if (value.Length > 255)
        {
            throw new ArgumentException("Product name cannot exceed 255 characters", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public static ProductName Of(string value) => new(value);

    public override string ToString() => Value;
}
