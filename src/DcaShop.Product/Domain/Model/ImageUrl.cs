using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>URL of the product's display image; empty for products without an image.</summary>
public sealed record ImageUrl : IValue
{
    public ImageUrl(string value)
    {
        Value = value ?? string.Empty;
    }

    public string Value { get; }

    public bool IsEmpty => Value.Length == 0;

    public static ImageUrl Of(string value) => new(value);

    public static ImageUrl None() => new(string.Empty);

    public override string ToString() => Value;
}
