using DcaShop.Product.Application.Shared;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Product.Application.CreateProduct;

public sealed class CreateProductUseCase : ICreateProductInputPort
{
    private readonly IProductRepository _products;
    private readonly ProductFactory _factory;
    private readonly IDomainEventPublisher _events;

    public CreateProductUseCase(IProductRepository products, ProductFactory factory, IDomainEventPublisher events)
    {
        _products = products;
        _factory = factory;
        _events = events;
    }

    public async Task<CreateProductResult> ExecuteAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        var sku = Sku.Of(command.Sku);
        if (await _products.FindBySkuAsync(sku, cancellationToken).ConfigureAwait(false) is not null)
        {
            throw new InvalidOperationException($"A product with SKU {sku} already exists");
        }

        var product = _factory.Create(
            sku,
            ProductName.Of(command.Name),
            ProductDescription.Of(command.Description),
            Category.Of(command.Category),
            ImageUrl.Of(command.ImageUrl),
            Price.Of(Money.Of(command.PriceAmount, command.PriceCurrency)),
            command.StockQuantity);

        await _products.SaveAsync(product, cancellationToken).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(product, cancellationToken).ConfigureAwait(false);

        return new CreateProductResult(product.Id.Value, product.Sku.Value, product.Name.Value);
    }
}
