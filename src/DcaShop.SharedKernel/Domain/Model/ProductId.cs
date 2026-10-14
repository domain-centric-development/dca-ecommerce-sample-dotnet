using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.SharedKernel.Domain.Model;

/// <summary>Identity of a product — the one identifier every context agrees on.</summary>
public readonly record struct ProductId(Guid Value) : IId
{
    public static ProductId Generate() => new(Guid.NewGuid());

    public static ProductId Of(string value) => new(Guid.Parse(value));

    /// <summary>The product id a caller's text names, if the text is shaped like one at all.</summary>
    public static bool TryOf(string? value, out ProductId productId)
    {
        var parsed = Guid.TryParse(value, out var guid);
        productId = parsed ? new ProductId(guid) : default;
        return parsed;
    }

    public override string ToString() => Value.ToString();
}