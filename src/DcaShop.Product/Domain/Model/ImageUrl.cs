using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>URL of the product's display image, at most 2000 characters; empty for products without an image.</summary>
public sealed record ImageUrl : IValue
{
    public ImageUrl(string value)
    {
        value ??= string.Empty;
        if (value.Length > 2000)
        {
            throw new ArgumentException("Image URL cannot exceed 2000 characters", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public bool IsEmpty => Value.Length == 0;

    public static ImageUrl Of(string value) => new(value);

    public static ImageUrl None() => new(string.Empty);

    public override string ToString() => Value;
}