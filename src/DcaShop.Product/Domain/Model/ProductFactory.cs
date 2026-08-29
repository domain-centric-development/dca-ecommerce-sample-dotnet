using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>
/// Creates new products. The initial price and stock are not attributes of the product — they are handed
/// to the Pricing and Inventory contexts through <c>ProductCreated</c>.
/// </summary>
public sealed class ProductFactory : IFactory
{
    public Product Create(Sku sku, ProductName name, ProductDescription description, Category category, ImageUrl imageUrl, Price initialPrice, int initialStock)
    {
        if (initialStock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialStock), "Initial stock cannot be negative");
        }

        var product = new Product(ProductId.Generate(), sku, name, description, category, imageUrl);
        product.RaiseCreated(initialPrice, initialStock);
        return product;
    }
}
