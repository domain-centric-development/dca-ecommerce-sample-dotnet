using System.Text.RegularExpressions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>Stock keeping unit — the unique business article key (uppercase letters, digits, hyphens).</summary>
public sealed partial record Sku : IValue
{
    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SKU cannot be blank", nameof(value));
        }

        if (!Pattern().IsMatch(value))
        {
            throw new ArgumentException("SKU must contain only uppercase letters, numbers, and hyphens", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static Sku Of(string value) => new(value);

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9-]+$")]
    private static partial Regex Pattern();
}
