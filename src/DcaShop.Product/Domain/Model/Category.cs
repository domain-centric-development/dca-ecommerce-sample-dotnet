using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>Assortment category a product is assigned to. Predefined categories are factory methods; open to more.</summary>
public sealed record Category : IValue
{
    public Category(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be blank", nameof(name));
        }

        Name = name.Trim();
    }

    public string Name { get; }

    public static Category Of(string name) => new(name);

    public static Category Books() => new("Books");

    public static Category Modeling() => new("Modeling");

    public static Category Apparel() => new("Apparel");

    public static Category DeskAndOffice() => new("Desk & Office");

    public static Category StickersAndPins() => new("Stickers & Pins");

    public override string ToString() => Name;
}
