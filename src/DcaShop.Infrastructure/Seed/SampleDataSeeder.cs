using DcaShop.Product.Adapter.Outgoing.Inventory;
using DcaShop.Product.Adapter.Outgoing.Pricing;
using DcaShop.Product.Application.CreateProduct;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DcaShop.Infrastructure.Seed;

/// <summary>
/// Fills the in-memory catalog at start-up. Prices and stock go to the stand-in adapters directly — once the
/// Pricing and Inventory contexts exist they will initialise themselves from <c>ProductCreatedEvent</c>.
/// </summary>
public sealed class SampleDataSeeder : IHostedService
{
    private static readonly (string Sku, string Name, string Description, string Category, decimal Price, int Stock)[] Products =
    {
        ("LAPTOP-001", "Laptop Pro 15", "A powerful 15-inch laptop for professionals.", "Electronics", 1299.99m, 10),
        ("PHONE-001", "Smartphone X", "Flagship smartphone with an outstanding camera.", "Electronics", 899.00m, 25),
        ("HEADPHONE-001", "Noise-Cancelling Headphones", "Over-ear headphones with active noise cancelling.", "Electronics", 249.50m, 40),
        ("BOOK-DDD", "Domain-Driven Design", "Tackling complexity in the heart of software.", "Books", 54.90m, 100),
        ("BOOK-IDDD", "Implementing Domain-Driven Design", "The practical companion to the blue book.", "Books", 49.90m, 60),
        ("SHIRT-001", "Hexagon T-Shirt", "Soft cotton shirt with a ports-and-adapters print.", "Clothing", 24.99m, 0),
    };

    private readonly IServiceScopeFactory _scopes;

    public SampleDataSeeder(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var createProduct = scope.ServiceProvider.GetRequiredService<ICreateProductInputPort>();
        var pricing = scope.ServiceProvider.GetRequiredService<InMemoryPricingDataAdapter>();
        var stock = scope.ServiceProvider.GetRequiredService<InMemoryStockDataAdapter>();

        foreach (var p in Products)
        {
            var created = await createProduct.ExecuteAsync(
                new CreateProductCommand(p.Sku, p.Name, p.Description, ImageUrlFor(p.Sku), p.Price, "EUR", p.Category, p.Stock),
                cancellationToken).ConfigureAwait(false);
            pricing.Seed(new ProductId(created.ProductId), Money.Euro(p.Price));
            stock.Seed(new ProductId(created.ProductId), p.Stock);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static string ImageUrlFor(string sku) => $"https://placehold.co/400x300?text={Uri.EscapeDataString(sku)}";
}
