using DcaShop.Product.Application.CreateProduct;
using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DcaShop.Infrastructure.Seed;

/// <summary>
/// Fills the in-memory catalog at start-up. Only products are created here: the price and the stock of each
/// product are set by the Pricing and Inventory contexts when they receive <c>ProductCreatedEvent</c>. Delivery is
/// asynchronous, so the seeder waits until the outbox has no pending publication left — a start-up convenience, not
/// a pattern: nothing else in the shop waits for an integration event.
/// </summary>
public sealed class SampleDataSeeder : IHostedService
{
    private static readonly (string Sku, string Name, string Description, string ImageUrl, string Category, decimal Price, int Stock)[] Products =
    {
        // Electronics
        ("LAPTOP-001", "Professional Laptop", "Unleash your productivity with this high-performance laptop featuring 16GB RAM, a blazing-fast 512GB SSD, and a stunning 15.6-inch Retina display. Built for professionals who demand power and portability, it delivers all-day battery life and whisper-quiet operation.", "/images/products/laptop.svg", "Electronics", 1299.99m, 15),
        ("PHONE-001", "Smartphone Pro", "Capture every moment in breathtaking detail with our flagship smartphone. The triple-lens 108MP camera system, edge-to-edge AMOLED display, and 5G connectivity make this the ultimate mobile companion for work and play.", "/images/products/smartphone.svg", "Electronics", 899.99m, 25),
        ("TABLET-001", "Tablet Air", "The perfect blend of power and portability. This ultra-lightweight tablet features a vibrant 11-inch display, Apple M2 chip, and supports stylus input for creative professionals. Ideal for sketching, note-taking, and streaming on the go.", "/images/products/tablet.svg", "Electronics", 599.99m, 30),
        // Clothing
        ("SHIRT-001", "Cotton T-Shirt", "Made from 100% organic combed cotton, this premium t-shirt offers unmatched softness and breathability. Available in 12 vibrant colors with a modern relaxed fit that looks great whether you dress it up or keep it casual.", "/images/products/tshirt.svg", "Clothing", 29.99m, 100),
        ("JEANS-001", "Classic Jeans", "Crafted from premium selvedge denim with a classic straight-leg fit that never goes out of style. Features reinforced stitching, copper rivets, and a comfortable mid-rise waist. These jeans only get better with age.", "/images/products/jeans.svg", "Clothing", 79.99m, 50),
        // Books
        ("BOOK-001", "Domain-Driven Design", "The seminal work by Eric Evans that introduced the software industry to Domain-Driven Design. This essential guide teaches you how to tackle complexity in the heart of software by connecting implementation to an evolving model of the business domain.", "/images/products/ddd-book.svg", "Books", 54.99m, 20),
        ("BOOK-002", "Clean Architecture", "Robert C. Martin's definitive guide to software structure and design. Learn the universal rules of software architecture that dramatically improve developer productivity throughout the life of any software system.", "/images/products/clean-architecture-book.svg", "Books", 39.99m, 35),
        // Home & Garden
        ("CHAIR-001", "Ergonomic Office Chair", "Designed in collaboration with orthopedic specialists, this premium office chair features adjustable lumbar support, breathable mesh back, and a 4D armrest system. Work in comfort for hours with proper spinal alignment and pressure distribution.", "/images/products/office-chair.svg", "Home & Garden", 299.99m, 12),
        ("DESK-001", "Standing Desk", "Transform your workspace with this electric height-adjustable standing desk. Smooth dual-motor system transitions between sitting and standing in seconds, with programmable memory presets. The spacious 60x30 inch bamboo surface provides plenty of room for dual monitors.", "/images/products/standing-desk.svg", "Home & Garden", 499.99m, 8),
        // Sports
        ("YOGA-001", "Yoga Mat Premium", "Elevate your practice with this professional-grade yoga mat. The dual-layer design provides superior cushioning and a non-slip surface that grips better the more you sweat. Includes a cotton carrying strap and is made from eco-friendly, biodegradable natural rubber.", "/images/products/yoga-mat.svg", "Sports", 49.99m, 40),
        ("DUMBBELL-001", "Adjustable Dumbbells Set", "Replace an entire rack of weights with one smart set. These space-saving adjustable dumbbells let you switch between 5kg and 25kg in seconds with a simple twist-lock mechanism. Perfect for home workouts with professional-grade cast iron construction.", "/images/products/dumbbells.svg", "Sports", 199.99m, 18),
    };

    private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopes;
    private readonly IIntegrationEventOutbox _outbox;

    public SampleDataSeeder(IServiceScopeFactory scopes, IIntegrationEventOutbox outbox)
    {
        _scopes = scopes;
        _outbox = outbox;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var createProduct = scope.ServiceProvider.GetRequiredService<ICreateProductInputPort>();

        foreach (var p in Products)
        {
            await createProduct.ExecuteAsync(
                new CreateProductCommand(p.Sku, p.Name, p.Description, p.ImageUrl, p.Price, "EUR", p.Category, p.Stock),
                cancellationToken).ConfigureAwait(false);
        }

        await WaitForOutboxAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Waits until every publication the seeding caused has been delivered (or given up on).</summary>
    private async Task WaitForOutboxAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + DrainTimeout;
        while (_outbox.All().Any(publication => publication.Status == PublicationStatus.Pending))
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("Sample data seeding timed out waiting for the integration-event outbox to drain.");
            }

            await Task.Delay(25, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
