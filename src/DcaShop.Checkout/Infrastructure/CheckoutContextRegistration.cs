using DcaShop.Checkout.Adapter.Incoming.Event.CartSync;
using DcaShop.Checkout.Adapter.Outgoing.Cart;
using DcaShop.Checkout.Adapter.Outgoing.Event;
using DcaShop.Checkout.Adapter.Outgoing.Payment;
using DcaShop.Checkout.Adapter.Outgoing.Persistence;
using DcaShop.Checkout.Adapter.Outgoing.Product;
using DcaShop.Checkout.Application.CartSync.SyncCheckoutWithCart;
using DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;
using DcaShop.Checkout.Application.CheckoutCompletion.GetPaymentProviders;
using DcaShop.Checkout.Application.CheckoutCompletion.GetShippingOptions;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitDelivery;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Application.Session.GetConfirmedCheckoutSession;
using DcaShop.Checkout.Application.Session.StartCheckout;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Checkout.Infrastructure;

/// <summary>Wires the Checkout context.</summary>
public static class CheckoutContextRegistration
{
    public static IServiceCollection AddCheckoutContext(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Domain
        services.AddSingleton<CheckoutPricing>();
        services.AddSingleton<CheckoutCartFactory>();

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
        services.AddScoped<ICheckoutArticleDataPort, CompositeCheckoutArticleDataAdapter>();
        AddPaymentProvider(services, configuration);
        services.AddSingleton<IPaymentProviderRegistry, InMemoryPaymentProviderRegistry>();
        services.AddScoped<IEventListener, CheckoutConfirmedEventPublisher>();

        // Incoming event consumers
        services.AddScoped<IEventListener, CartChangeEventConsumer>();

        return services;
    }

    /// <summary>
    /// The payment service provider when its address is configured, the stand-in otherwise — one provider either way.
    /// </summary>
    private static void AddPaymentProvider(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(PaymentProviderOptions.SectionName).Get<PaymentProviderOptions>();
        if (options?.BaseUrl is not { } baseUrl)
        {
            services.AddSingleton<IPaymentProvider, MockPaymentProvider>();
            return;
        }

        services.AddHttpClient(RestPaymentProvider.HttpClientName, client =>
        {
            client.BaseAddress = WithTrailingSlash(baseUrl);
            client.Timeout = options.Timeout;
        });
        services.AddSingleton<IPaymentProvider, RestPaymentProvider>();
    }

    /// <summary>So the provider's paths resolve below its address instead of replacing its last segment.</summary>
    private static Uri WithTrailingSlash(Uri address) =>
        address.AbsoluteUri.EndsWith('/') ? address : new Uri(address.AbsoluteUri + "/");
}