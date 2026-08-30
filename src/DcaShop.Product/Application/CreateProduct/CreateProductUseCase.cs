using DomainCentric.BuildingBlocks.Application.Transactions;
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
    private readonly ITransactionBoundary _transactionBoundary;

    public CreateProductUseCase(IProductRepository products, ProductFactory factory, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _products = products;
        _factory = factory;
        _events = events;
    }

    public async Task<CreateProductResult> ExecuteAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        // Whole use case is local: one short transaction
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var sku = Sku.Of(command.Sku);
                if (await _products.FindBySkuAsync(sku, ct).ConfigureAwait(false) is not null)
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

                await _products.SaveAsync(product, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(product, ct).ConfigureAwait(false);

                return new CreateProductResult(product.Id.Value, product.Sku.Value, product.Name.Value);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
