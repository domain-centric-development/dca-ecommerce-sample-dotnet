using DcaShop.Backoffice.Adapter.Incoming.Web;
using DcaShop.Backoffice.Adapter.Outgoing.Persistence;
using DcaShop.Backoffice.Application.GetEventPublications;
using DcaShop.Backoffice.Application.Shared;
using DcaShop.Backoffice.Application.ReplayFailedPublication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DcaShop.Backoffice.Infrastructure;

/// <summary>Wires the Backoffice module, including its own authentication scheme.</summary>
public static class BackofficeContextRegistration
{
    public static IServiceCollection AddBackofficeContext(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<BackofficeOptions>(configuration.GetSection(BackofficeOptions.SectionName));

        // Use cases (input ports)
        services.AddScoped<IGetEventPublicationsInputPort, GetEventPublicationsUseCase>();

        services.AddScoped<IReplayFailedPublicationInputPort, ReplayFailedPublicationUseCase>();
        services.AddScoped<IEventPublicationRecoveryPort, OutboxEventPublicationRecoveryAdapter>();

        // Outgoing adapters (output ports)
        services.AddScoped<IEventPublicationLogStore, OutboxEventPublicationLogStore>();

        // The operator session: its own scheme, its own cookie, no overlap with the shop's identity. It is never
        // the default scheme — the shop's is (Account) — so every backoffice page names it in its [Authorize].
        services
            .AddAuthentication()
            .AddCookie(BackofficeOptions.AuthenticationScheme, options =>
            {
                options.Cookie.Name = BackofficeOptions.CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.Cookie.Path = BackofficeContext.PathPrefix;
                options.LoginPath = $"{BackofficeContext.PathPrefix}/login";
                options.AccessDeniedPath = $"{BackofficeContext.PathPrefix}/login";
            });

        services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, BackofficeCookieHardening>();

        return services;
    }

    /// <summary>
    /// Applies the two cookie settings that must come from configuration rather than from a literal: the
    /// <c>Secure</c> flag and the session lifetime.
    /// </summary>
    private sealed class BackofficeCookieHardening : IPostConfigureOptions<CookieAuthenticationOptions>
    {
        private readonly BackofficeOptions _options;

        public BackofficeCookieHardening(IOptions<BackofficeOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options.Value;
        }

        public void PostConfigure(string? name, CookieAuthenticationOptions options)
        {
            if (name != BackofficeOptions.AuthenticationScheme)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(options);
            options.Cookie.SecurePolicy = _options.SecureCookies
                ? Microsoft.AspNetCore.Http.CookieSecurePolicy.Always
                : Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
            options.ExpireTimeSpan = _options.SessionLifetime;
        }
    }
}
