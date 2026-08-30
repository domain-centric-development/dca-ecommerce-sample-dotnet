using DcaShop.Cart.Adapter.Incoming.Event;
using DcaShop.Cart.Adapter.Outgoing.Event;
using DcaShop.Cart.Adapter.Outgoing.Persistence;
using DcaShop.Cart.Adapter.Outgoing.Product;
using DcaShop.Cart.Api;
using DcaShop.Cart.Application.AddItemToCart;
using DcaShop.Cart.Application.CheckoutCart;
using DcaShop.Cart.Application.CompleteCart;
using DcaShop.Cart.Application.CreateCart;
using DcaShop.Cart.Application.GetActiveCart;
using DcaShop.Cart.Application.GetCartById;
using DcaShop.Cart.Application.GetOrCreateActiveCart;
using DcaShop.Cart.Application.RemoveItemFromCart;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Cart.Infrastructure;

/// <summary>Wires the Shopping Cart context.</summary>
public static class CartContextRegistration
{
    public static IServiceCollection AddCartContext(this IServiceCollection services)
    {
        // Domain
        services.AddSingleton<EnrichedCartFactory>();

        // Use cases (input ports)
        services.AddScoped<ICreateCartInputPort, CreateCartUseCase>();
        services.AddScoped<IGetOrCreateActiveCartInputPort, GetOrCreateActiveCartUseCase>();
        services.AddScoped<IGetCartByIdInputPort, GetCartByIdUseCase>();
        services.AddScoped<IGetActiveCartInputPort, GetActiveCartUseCase>();
        services.AddScoped<IAddItemToCartInputPort, AddItemToCartUseCase>();
        services.AddScoped<IRemoveItemFromCartInputPort, RemoveItemFromCartUseCase>();
        services.AddScoped<ICheckoutCartInputPort, CheckoutCartUseCase>();
        services.AddScoped<ICompleteCartInputPort, CompleteCartUseCase>();
        services.AddScoped<EnrichedCartReader>();

        // Outgoing adapters (output ports)
        services.AddSingleton<IShoppingCartRepository, InMemoryShoppingCartRepository>();
        services.AddScoped<IArticleDataPort, ProductCatalogArticleDataAdapter>();
        services.AddScoped<IEventListener, CartCheckedOutEventPublisher>();
        services.AddScoped<IEventListener, CartContentsChangedEventPublisher>();

        // Incoming event consumers
        services.AddScoped<IEventListener, CartCompletionEventConsumer>();

        // Published API
        services.AddScoped<CartService>();

        return services;
    }
}
