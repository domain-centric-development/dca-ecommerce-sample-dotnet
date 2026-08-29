using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Product.Domain.Model;

/// <summary>Read model: a product together with its current article data. Carries the cross-context rules "can be purchased" and "stock suffices".</summary>
public sealed record EnrichedProduct(
    ProductId ProductId,
    Sku Sku,
    ProductName Name,
    ProductDescription Description,
    Category Category,
    ImageUrl ImageUrl,
    ProductArticle Article) : IValue
{
    public static EnrichedProduct From(Product product, ProductArticle article) =>
        new(product.Id, product.Sku, product.Name, product.Description, product.Category, product.ImageUrl, article);

    public Money CurrentPrice => Article.CurrentPrice;

    public bool CanBePurchased => Article.IsAvailable && Article.AvailableStock > 0;

    public bool HasStockFor(int quantity) => Article.HasStockFor(quantity);
}
