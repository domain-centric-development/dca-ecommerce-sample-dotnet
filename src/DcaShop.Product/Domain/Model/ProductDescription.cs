using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>Marketing and detail text, at most 2000 characters, may be empty.</summary>
public sealed record ProductDescription : IValue
{
    public ProductDescription(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > 2000)
        {
            throw new ArgumentException("Product description cannot exceed 2000 characters", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static ProductDescription Of(string value) => new(value);

    public static ProductDescription Empty() => new(string.Empty);

    public override string ToString() => Value;
}
