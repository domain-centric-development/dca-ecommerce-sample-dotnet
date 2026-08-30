using DcaShop.Inventory.Adapter.Incoming.Event;
using DcaShop.Inventory.Adapter.Outgoing.Persistence;
using DcaShop.Inventory.Api;
using DcaShop.Inventory.Application.GetStockForProducts;
using DcaShop.Inventory.Application.ReduceStock;
using DcaShop.Inventory.Application.SetStockLevel;
using DcaShop.Inventory.Application.Shared;
using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Inventory.Infrastructure;

/// <summary>Wires the Inventory context.</summary>
public static class InventoryContextRegistration
{
    public static IServiceCollection AddInventoryContext(this IServiceCollection services)
    {
        // Use cases (input ports)
        services.AddScoped<ISetStockLevelInputPort, SetStockLevelUseCase>();
        services.AddScoped<IReduceStockInputPort, ReduceStockUseCase>();
        services.AddScoped<IGetStockForProductsInputPort, GetStockForProductsUseCase>();

        // Outgoing adapters (output ports)
        services.AddSingleton<IStockLevelRepository, InMemoryStockLevelRepository>();

        // Incoming event adapters
        services.AddScoped<IEventListener, StockInitializationEventConsumer>();
        services.AddScoped<IEventListener, StockReductionEventConsumer>();

        // Published API
        services.AddScoped<InventoryService>();

        return services;
    }
}
