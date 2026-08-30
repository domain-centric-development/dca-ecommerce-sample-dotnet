using DcaShop.Checkout.Adapter.Incoming.Event;
using DcaShop.Checkout.Adapter.Outgoing.Cart;
using DcaShop.Checkout.Adapter.Outgoing.Event;
using DcaShop.Checkout.Adapter.Outgoing.Payment;
using DcaShop.Checkout.Adapter.Outgoing.Persistence;
using DcaShop.Checkout.Adapter.Outgoing.Product;
using DcaShop.Checkout.Application.ConfirmCheckout;
using DcaShop.Checkout.Application.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.GetCheckoutSession;
using DcaShop.Checkout.Application.GetConfirmedCheckoutSession;
using DcaShop.Checkout.Application.GetPaymentProviders;
using DcaShop.Checkout.Application.GetShippingOptions;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Application.StartCheckout;
using DcaShop.Checkout.Application.SubmitBuyerInfo;
using DcaShop.Checkout.Application.SubmitDelivery;
using DcaShop.Checkout.Application.SubmitPayment;
using DcaShop.Checkout.Application.SyncCheckoutWithCart;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Checkout.Infrastructure;

/// <summary>Wires the Checkout context.</summary>
public static class CheckoutContextRegistration
{
    public static IServiceCollection AddCheckoutContext(this IServiceCollection services)
    {
        // Domain
        services.AddSingleton<CheckoutStepValidator>();

        // Use cases (input ports)
        services.AddScoped<IStartCheckoutInputPort, StartCheckoutUseCase>();
        services.AddScoped<IGetCheckoutSessionInputPort, GetCheckoutSessionUseCase>();
        services.AddScoped<IGetActiveCheckoutSessionInputPort, GetActiveCheckoutSessionUseCase>();
        services.AddScoped<IGetConfirmedCheckoutSessionInputPort, GetConfirmedCheckoutSessionUseCase>();
        services.AddScoped<ISubmitBuyerInfoInputPort, SubmitBuyerInfoUseCase>();
        services.AddScoped<ISubmitDeliveryInputPort, SubmitDeliveryUseCase>();
        services.AddScoped<IGetShippingOptionsInputPort, GetShippingOptionsUseCase>();
        services.AddScoped<ISubmitPaymentInputPort, SubmitPaymentUseCase>();
        services.AddScoped<IGetPaymentProvidersInputPort, GetPaymentProvidersUseCase>();
        services.AddScoped<IConfirmCheckoutInputPort, ConfirmCheckoutUseCase>();
        services.AddScoped<ISyncCheckoutWithCartInputPort, SyncCheckoutWithCartUseCase>();

        // Outgoing adapters (output ports)
        services.AddSingleton<ICheckoutSessionRepository, InMemoryCheckoutSessionRepository>();
        services.AddScoped<ICartDataPort, CartDataAdapter>();
        services.AddScoped<ICheckoutArticleDataPort, ProductCatalogCheckoutArticleDataAdapter>();
        services.AddSingleton<IPaymentProviderRegistry, InMemoryPaymentProviderRegistry>();
        services.AddScoped<IEventListener, CheckoutConfirmedEventPublisher>();

        // Incoming event consumers
        services.AddScoped<IEventListener, CartChangeEventConsumer>();

        return services;
    }
}
