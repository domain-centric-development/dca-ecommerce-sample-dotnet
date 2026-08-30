using DcaShop.Cart.Infrastructure;
using DcaShop.Checkout.Infrastructure;
using DcaShop.Infrastructure.Events;
using DcaShop.Infrastructure.Seed;
using DcaShop.Product.Infrastructure;
using DcaShop.SharedKernel.Adapter.Outgoing.Event;
using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DcaShop.Infrastructure;

/// <summary>Composes the whole shop: shared event plumbing, every bounded context, sample data.</summary>
public static class DcaShopRegistration
{
    public static IServiceCollection AddDcaShop(this IServiceCollection services)
    {
        // Event plumbing (shared kernel)
        services.AddScoped<IEventDispatcher, InProcessEventDispatcher>();
        services.AddScoped<IDomainEventPublisher, InProcessDomainEventPublisher>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IIntegrationEventOutbox, InMemoryIntegrationEventOutbox>();
        services.AddSingleton<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();
        services.TryAddSingleton(IntegrationEventRetryPolicy.Default);
        services.AddHostedService<IntegrationEventDispatcherService>();

        // Bounded contexts
        services.AddProductContext();
        services.AddCartContext();
        services.AddCheckoutContext();

        // Sample data
        services.AddHostedService<SampleDataSeeder>();

        return services;
    }
}
