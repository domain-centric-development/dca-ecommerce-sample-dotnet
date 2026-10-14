using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>Marketing and detail text, at most 2000 characters, may be empty.</summary>
public sealed record ProductDescription : IValue
{
    /// <summary>The longest text the shop keeps.</summary>
    public const int MaxLength = 2000;

    public ProductDescription(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"Product description cannot exceed {MaxLength} characters", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static ProductDescription Of(string value) => new(value);

    public static ProductDescription Empty() => new(string.Empty);

    public override string ToString() => Value;
}