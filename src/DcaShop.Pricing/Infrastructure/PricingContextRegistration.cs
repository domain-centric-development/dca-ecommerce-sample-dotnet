using DcaShop.Pricing.Adapter.Incoming.Event;
using DcaShop.Pricing.Adapter.Outgoing.Persistence;
using DcaShop.Pricing.Api;
using DcaShop.Pricing.Application.GetPricesForProducts;
using DcaShop.Pricing.Application.SetProductPrice;
using DcaShop.Pricing.Application.Shared;
using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Pricing.Infrastructure;

/// <summary>Wires the Pricing context.</summary>
public static class PricingContextRegistration
{
    public static IServiceCollection AddPricingContext(this IServiceCollection services)
    {
        // Use cases (input ports)
        services.AddScoped<ISetProductPriceInputPort, SetProductPriceUseCase>();
        services.AddScoped<IGetPricesForProductsInputPort, GetPricesForProductsUseCase>();

        // Outgoing adapters (output ports)
        services.AddSingleton<IProductPriceRepository, InMemoryProductPriceRepository>();

        // Incoming event adapters
        services.AddScoped<IEventListener, PriceInitializationEventConsumer>();

        // Published API
        services.AddScoped<PricingService>();

        return services;
    }
}
