using DcaShop.Cart.Adapter.Incoming.Api;
using DcaShop.Cart.Adapter.Incoming.Event.CartCheckout;
using DcaShop.Cart.Adapter.Outgoing.Event;
using DcaShop.Cart.Adapter.Outgoing.Persistence;
using DcaShop.Cart.Adapter.Outgoing.Product;
using DcaShop.Cart.Api;
using DcaShop.Cart.Application.CartCheckout.CompleteCart;
using DcaShop.Cart.Application.CartRecovery.GetCartMergeOptions;
using DcaShop.Cart.Application.CartRecovery.MergeCarts;
using DcaShop.Cart.Application.CartRecovery.RecoverCartOnLogin;
using DcaShop.Cart.Application.Operations.GetAllCarts;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.CreateCart;
using DcaShop.Cart.Application.Shopping.GetActiveCart;
using DcaShop.Cart.Application.Shopping.GetCartById;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Cart.Application.Shopping.RemoveItemFromCart;
using DcaShop.Cart.Domain.Model;
using DcaShop.Cart.Domain.Service;
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
        services.AddScoped<IGetAllCartsInputPort, GetAllCartsUseCase>();
        services.AddScoped<IAddItemToCartInputPort, AddItemToCartUseCase>();
        services.AddScoped<IRemoveItemFromCartInputPort, RemoveItemFromCartUseCase>();
        services.AddScoped<ICompleteCartInputPort, CompleteCartUseCase>();
        services.AddScoped<IGetCartMergeOptionsInputPort, GetCartMergeOptionsUseCase>();
        services.AddScoped<IMergeCartsInputPort, MergeCartsUseCase>();
        services.AddScoped<IRecoverCartOnLoginInputPort, RecoverCartOnLoginUseCase>();
        services.AddScoped<EnrichedCartReader>();

        // Incoming adapters
        services.AddSingleton<ShoppingCartDtoConverter>();

        // The one place in this context that turns a failure into an HTTP answer
        services.AddExceptionHandler<CartApiExceptionHandler>();

        // Outgoing adapters (output ports)
        services.AddSingleton<IShoppingCartRepository, InMemoryShoppingCartRepository>();
        services.AddScoped<IArticleDataPort, CompositeArticleDataAdapter>();
        services.AddScoped<IEventListener, CartContentsChangedEventPublisher>();

        // Incoming event consumers
        services.AddScoped<IEventListener, CartCompletionEventConsumer>();

        // Published API
        services.AddScoped<CartService>();

        return services;
    }
}