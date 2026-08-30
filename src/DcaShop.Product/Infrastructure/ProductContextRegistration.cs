using DcaShop.Product.Adapter.Incoming.Api;
using DcaShop.Product.Adapter.Incoming.Mcp;
using DcaShop.Product.Adapter.Outgoing.Event;
using DcaShop.Product.Adapter.Outgoing.Inventory;
using DcaShop.Product.Adapter.Outgoing.Persistence;
using DcaShop.Product.Adapter.Outgoing.Pricing;
using DcaShop.Product.Api;
using DcaShop.Product.Application.CreateProduct;
using DcaShop.Product.Application.GetAllProducts;
using DcaShop.Product.Application.GetProductById;
using DcaShop.Product.Application.Shared;
using DcaShop.Product.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Product.Infrastructure;

/// <summary>Wires the Product Catalog context: the .NET counterpart of Spring's component scan, made explicit.</summary>
public static class ProductContextRegistration
{
    public static IServiceCollection AddProductContext(this IServiceCollection services)
    {
        // Domain
        services.AddSingleton<ProductFactory>();

        // Use cases (input ports)
        services.AddScoped<ICreateProductInputPort, CreateProductUseCase>();
        services.AddScoped<IGetAllProductsInputPort, GetAllProductsUseCase>();
        services.AddScoped<IGetProductByIdInputPort, GetProductByIdUseCase>();
        services.AddScoped<ProductArticleAssembler>();

        // Outgoing adapters (output ports)
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        services.AddScoped<IPricingDataPort, PricingDataAdapter>();
        services.AddScoped<IProductStockDataPort, InventoryStockDataAdapter>();
        services.AddScoped<IEventListener, ProductCreatedEventPublisher>();

        // Incoming adapters
        services.AddSingleton<ProductDtoConverter>();

        // The catalog as MCP tools. The transport is HTTP; the host maps it at /mcp.
        services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<ProductCatalogMcpToolProvider>();

        // Published API
        services.AddScoped<ProductCatalogService>();

        return services;
    }
}
