using DcaShop.Checkout.Events;
using DcaShop.Inventory.Adapter.Incoming.Event;
using DcaShop.Inventory.Api;
using DcaShop.Inventory.Application.ReduceStock;
using DcaShop.Inventory.Application.SetStockLevel;
using DcaShop.Pricing.Adapter.Incoming.Event;
using DcaShop.Pricing.Api;
using DcaShop.Product.Events;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Delivery is at least once: each consumer of a trigger deduplicates on the event id, so the same event delivered
/// twice changes the consuming context's state once. Each test changes the state between the two deliveries, so a
/// consumer that acted again would be seen. The record is per effect: two effects of one event both happen.
/// </summary>
public sealed class RedeliveredTriggerTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RedeliveredTriggerTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ARedeliveredCheckoutConfirmationReducesStockOnce()
    {
        var product = ProductId.Generate();
        await Inventory().SetStockLevelAsync(product, 10);
        var confirmed = new CheckoutConfirmedEvent(
            Guid.NewGuid(), DateTimeOffset.UtcNow, "session-1", Guid.NewGuid().ToString(), "customer-1", Money.Euro(20m),
            [new CheckoutConfirmedEvent.LineItemInfo(product, 2)]);

        await DeliverAsync<StockReductionEventConsumer>(confirmed);
        await DeliverAsync<StockReductionEventConsumer>(confirmed);

        Assert.Equal(8, (await Inventory().GetStockAsync(product))!.AvailableStock);
    }

    [Fact]
    public async Task ARedeliveredProductCreationDoesNotResetTheStock()
    {
        var created = ProductCreated(out var product);

        await DeliverAsync<StockInitializationEventConsumer>(created);
        await Inventory().ReduceStockAsync(product, 2);
        await DeliverAsync<StockInitializationEventConsumer>(created);

        Assert.Equal(5, (await Inventory().GetStockAsync(product))!.AvailableStock);
    }

    [Fact]
    public async Task ARedeliveredProductCreationDoesNotResetThePrice()
    {
        var created = ProductCreated(out var product);

        await DeliverAsync<PriceInitializationEventConsumer>(created);
        await Pricing().SetPriceAsync(product, Money.Euro(12.00m));
        await DeliverAsync<PriceInitializationEventConsumer>(created);

        Assert.Equal(Money.Euro(12.00m), (await Pricing().GetPriceAsync(product))!.CurrentPrice);
    }

    [Fact]
    public async Task TwoInventoryEffectsOfTheSameEventBothHappen()
    {
        var product = ProductId.Generate();
        var eventId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<ISetStockLevelInputPort>()
            .ExecuteAsync(new SetStockLevelCommand(product.Value, 10, eventId));
        var reduced = await scope.ServiceProvider.GetRequiredService<IReduceStockInputPort>()
            .ExecuteAsync(new ReduceStockCommand([new ReduceStockCommand.Line(product.Value, 2)], eventId));

        Assert.False(reduced.AlreadyReduced);
        Assert.Equal(8, (await Inventory().GetStockAsync(product))!.AvailableStock);
    }

    [Fact]
    public async Task PricingKeysAProcessedEventByItsEffect()
    {
        var processedEvents = _factory.Services.GetRequiredService<Pricing.Application.Shared.IProcessedEvents>();
        var eventId = Guid.NewGuid();

        await processedEvents.MarkProcessedAsync("set-product-price", eventId);

        Assert.True(await processedEvents.ContainsAsync("set-product-price", eventId));
        Assert.False(await processedEvents.ContainsAsync("another-effect", eventId));
    }

    private static ProductCreatedEvent ProductCreated(out ProductId product)
    {
        product = ProductId.Generate();
        return new ProductCreatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, product.Value, "10.00", "EUR", 7);
    }

    private async Task DeliverAsync<TConsumer>(object @event)
        where TConsumer : IEventListener
    {
        using var scope = _factory.Services.CreateScope();
        var consumer = scope.ServiceProvider.GetServices<IEventListener>().OfType<TConsumer>().Single();
        await consumer.OnAsync(@event);
    }

    private InventoryService Inventory() => _factory.Services.CreateScope().ServiceProvider.GetRequiredService<InventoryService>();

    private PricingService Pricing() => _factory.Services.CreateScope().ServiceProvider.GetRequiredService<PricingService>();
}