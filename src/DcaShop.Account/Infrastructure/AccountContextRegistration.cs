using DcaShop.Account.Adapter.Outgoing.Persistence;
using DcaShop.Account.Adapter.Outgoing.Security;
using DcaShop.Account.Application.AuthenticateAccount;
using DcaShop.Account.Application.ChangePassword;
using DcaShop.Account.Application.ChangeProfile;
using DcaShop.Account.Application.GetAccountOverview;
using DcaShop.Account.Application.GetProfile;
using DcaShop.Account.Application.RegisterAccount;
using DcaShop.Account.Application.Shared;
using DcaShop.Account.Domain.Gateway;
using DcaShop.SharedKernel.Application.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Account.Infrastructure;

/// <summary>Wires the Account context.</summary>
public static class AccountContextRegistration
{
    public static IServiceCollection AddAccountContext(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddHttpContextAccessor();

        // Domain
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        // Use cases (input ports)
        services.AddScoped<IRegisterAccountInputPort, RegisterAccountUseCase>();
        services.AddScoped<IAuthenticateAccountInputPort, AuthenticateAccountUseCase>();
        services.AddScoped<IChangePasswordInputPort, ChangePasswordUseCase>();
        services.AddScoped<IChangeProfileInputPort, ChangeProfileUseCase>();
        services.AddScoped<IGetProfileInputPort, GetProfileUseCase>();
        services.AddScoped<IGetAccountOverviewInputPort, GetAccountOverviewUseCase>();

        // Outgoing adapters (output ports)
        services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
        services.AddSingleton<JwtTokenService>();
        services.AddSingleton<ITokenService>(sp => sp.GetRequiredService<JwtTokenService>());
        services.AddScoped<IIdentitySession, JwtIdentitySession>();
        services.AddScoped<IRegisteredUserValidator, AccountBasedRegisteredUserValidator>();

        // The identity port is declared in the shared kernel because every context keys its data on the UserId,
        // but only Account can resolve one — see the port's own remarks.
        services.AddScoped<IIdentityProvider, HttpContextIdentityProvider>();

        // The shop's identity as ASP.NET Core authentication (ADR-008). One handler, two schemes: the browser
        // scheme reads the two cookies of ADR-006 and never the Authorization header; the API scheme reads the
        // header and never a cookie, which is what lets /api/** and /mcp skip the antiforgery token (ADR-007).
        // The default is a policy scheme that picks one of them by request path — so [Authorize] on a resource
        // challenges with 401, and [Authorize] on a page redirects to the login form, without either naming a
        // scheme. The backoffice registers its own cookie scheme next to these and names it explicitly.
        services
            .AddAuthentication(ShopPrincipal.Scheme)
            .AddPolicyScheme(ShopPrincipal.Scheme, "Shop identity", options =>
                options.ForwardDefaultSelector = context =>
                    TokenOnlyPaths.IsTokenOnlyEndpoint(context.Request.Path)
                        ? ShopPrincipal.BearerScheme
                        : ShopPrincipal.CookieScheme)
            .AddScheme<ShopIdentityAuthenticationOptions, ShopIdentityAuthenticationHandler>(
                ShopPrincipal.CookieScheme, options => options.BearerOnly = false)
            .AddScheme<ShopIdentityAuthenticationOptions, ShopIdentityAuthenticationHandler>(
                ShopPrincipal.BearerScheme, options => options.BearerOnly = true);
        services.AddAuthorization();

        return services;
    }
}
