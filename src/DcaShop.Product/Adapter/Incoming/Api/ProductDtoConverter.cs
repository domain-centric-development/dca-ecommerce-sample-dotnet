using DcaShop.Product.Application.CreateProduct;
using DcaShop.Product.Domain.Model;

namespace DcaShop.Product.Adapter.Incoming.Api;

/// <summary>
/// Translates between the use cases' models and the REST representation. It belongs to the adapter layer: the
/// application layer knows nothing about JSON, and the domain read model knows nothing about either.
/// </summary>
public sealed class ProductDtoConverter
{
    /// <summary>
    /// The answer to a create. Price and stock stay empty — they are owned by other contexts and are not known
    /// yet at the moment the product comes into existence.
    /// </summary>
    public ProductDto ToDto(CreateProductResult result, CreateProductCommand command)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(command);
        return new ProductDto(
            result.ProductId.ToString(),
            result.Sku,
            result.Name,
            command.Description,
            command.ImageUrl,
            null,
            null,
            command.Category,
            null,
            null);
    }

    public ProductDto ToDto(EnrichedProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);
        return new ProductDto(
            product.ProductId.Value.ToString(),
            product.Sku.Value,
            product.Name.Value,
            product.Description.Value,
            product.ImageUrl.Value,
            product.CurrentPrice.Amount,
            product.CurrentPrice.Currency,
            product.Category.Name,
            product.Article.AvailableStock,
            product.Article.IsAvailable);
    }
}
