using DcaShop.Account.Infrastructure;
using DcaShop.Backoffice.Infrastructure;
using DcaShop.Cart.Infrastructure;
using DcaShop.Checkout.Infrastructure;
using DcaShop.Infrastructure.Events;
using DcaShop.Infrastructure.Persistence;
using DcaShop.Inventory.Infrastructure;
using DcaShop.Portal.Infrastructure;
using DcaShop.Pricing.Infrastructure;
using DcaShop.Product.Adapter.Incoming.Bootstrap;
using DcaShop.Product.Infrastructure;
using DcaShop.SharedKernel.Adapter.Outgoing.Event;
using DcaShop.SharedKernel.Infrastructure.Events;
using DcaShop.SharedKernel.Infrastructure.Transactions;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Npgsql;

namespace DcaShop.Infrastructure;

/// <summary>Composes the whole shop: shared event plumbing, every bounded context, sample data.</summary>
public static class DcaShopRegistration
{
    public static IServiceCollection AddDcaShop(this IServiceCollection services, IConfiguration configuration)
    {
        // The database: migrated before any other hosted service starts, then shared by every context's adapters
        services.AddHostedService<DatabaseMigration>();
        services.AddSingleton(_ => NpgsqlDataSource.Create(ShopDatabase.ConnectionString(configuration)));
        services.AddSingleton<SharedKernel.Infrastructure.Persistence.Database>();

        // Event plumbing (shared kernel)
        services.AddScoped<IEventDispatcher, InProcessEventDispatcher>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IEventOutbox, PostgresEventOutbox>();
        services.AddScoped<IIntegrationEventPublisher, OutboxIntegrationEventPublisher>();

        // Transaction boundary: writing use cases run inside ITransactionBoundary.InTransactionAsync
        // — one PostgreSQL transaction, current for the flow that opened it
        services.AddSingleton<PostgresTransactionBoundary>();
        services.AddSingleton<ITransactionBoundary>(sp => sp.GetRequiredService<PostgresTransactionBoundary>());
        services.AddSingleton<ITransactionHooks>(sp => sp.GetRequiredService<PostgresTransactionBoundary>());
        services.TryAddSingleton(EventRetryPolicy.Default);
        services.AddHostedService<EventDispatcherService>();

        // Bounded contexts. Account comes first: every other context reads the visitor identity it resolves.
        services.AddAccountContext(configuration);
        services.AddPortalContext();
        services.AddPricingContext();
        services.AddInventoryContext();
        services.AddProductContext();
        services.AddCartContext();
        services.AddCheckoutContext(configuration);

        // Generic subdomain: operating the application itself
        services.AddBackofficeContext(configuration);

        // Sample data
        services.AddHostedService<SampleDataSeeder>();

        return services;
    }
}