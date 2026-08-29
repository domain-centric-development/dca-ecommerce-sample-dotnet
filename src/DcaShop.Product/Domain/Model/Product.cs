using DcaShop.Product.Domain.Event;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>
/// An identifiable product listed in the catalog with descriptive master data. Prices and stock are not
/// part of this aggregate — they live in the Pricing and Inventory contexts.
/// </summary>
public sealed class Product : AggregateRootBase<Product, ProductId>
{
    public Product(ProductId id, Sku sku, ProductName name, ProductDescription description, Category category, ImageUrl imageUrl)
    {
        Id = id;
        Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        ImageUrl = imageUrl ?? throw new ArgumentNullException(nameof(imageUrl));
    }

    public override ProductId Id { get; }

    public Sku Sku { get; }

    public ProductName Name { get; private set; }

    public ProductDescription Description { get; private set; }

    public Category Category { get; private set; }

    public ImageUrl ImageUrl { get; }

    public void UpdateName(ProductName newName)
    {
        ArgumentNullException.ThrowIfNull(newName);
        var oldName = Name;
        Name = newName;
        RegisterEvent(ProductNameChanged.Now(Id, oldName, newName));
    }

    public void UpdateDescription(ProductDescription newDescription)
    {
        ArgumentNullException.ThrowIfNull(newDescription);
        var oldDescription = Description;
        Description = newDescription;
        RegisterEvent(ProductDescriptionChanged.Now(Id, oldDescription, newDescription));
    }

    public void UpdateCategory(Category newCategory)
    {
        ArgumentNullException.ThrowIfNull(newCategory);
        var oldCategory = Category;
        Category = newCategory;
        RegisterEvent(ProductCategoryChanged.Now(Id, oldCategory, newCategory));
    }

    /// <summary>Only the factory raises <see cref="ProductCreated"/>: creation carries data (price, stock) the aggregate does not own.</summary>
    internal void RaiseCreated(Price initialPrice, int initialStock) =>
        RegisterEvent(ProductCreated.Now(Id, Sku, Name, Category, initialPrice, initialStock));
}
