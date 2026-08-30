using DomainCentric.BuildingBlocks.Application.Transactions;
using DcaShop.Account.Infrastructure;
using DcaShop.Backoffice.Infrastructure;
using DcaShop.Cart.Infrastructure;
using DcaShop.Checkout.Infrastructure;
using DcaShop.Infrastructure.Events;
using DcaShop.Infrastructure.Seed;
using DcaShop.Inventory.Infrastructure;
using DcaShop.Portal.Infrastructure;
using DcaShop.Pricing.Infrastructure;
using DcaShop.Product.Infrastructure;
using DcaShop.SharedKernel.Adapter.Outgoing.Event;
using DcaShop.SharedKernel.Infrastructure.Transactions;
using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DcaShop.Infrastructure;

/// <summary>Composes the whole shop: shared event plumbing, every bounded context, sample data.</summary>
public static class DcaShopRegistration
{
    public static IServiceCollection AddDcaShop(this IServiceCollection services, IConfiguration configuration)
    {
        // Event plumbing (shared kernel)
        services.AddScoped<IEventDispatcher, InProcessEventDispatcher>();
        services.AddScoped<IDomainEventPublisher, InProcessDomainEventPublisher>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IIntegrationEventOutbox, InMemoryIntegrationEventOutbox>();
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

        // Transaction boundary: writing use cases run inside ITransactionBoundary.InTransactionAsync
        services.AddScoped<InMemoryTransactionBoundary>();
        services.AddScoped<ITransactionBoundary>(sp => sp.GetRequiredService<InMemoryTransactionBoundary>());
        services.AddScoped<ITransactionHooks>(sp => sp.GetRequiredService<InMemoryTransactionBoundary>());
        services.TryAddSingleton(IntegrationEventRetryPolicy.Default);
        services.AddHostedService<IntegrationEventDispatcherService>();

        // Bounded contexts. Account comes first: every other context reads the visitor identity it resolves.
        services.AddAccountContext(configuration);
        services.AddPortalContext();
        services.AddPricingContext();
        services.AddInventoryContext();
        services.AddProductContext();
        services.AddCartContext();
        services.AddCheckoutContext();

        // Operational modules (not bounded contexts)
        services.AddBackofficeModule(configuration);

        // Sample data
        services.AddHostedService<SampleDataSeeder>();

        return services;
    }
}
