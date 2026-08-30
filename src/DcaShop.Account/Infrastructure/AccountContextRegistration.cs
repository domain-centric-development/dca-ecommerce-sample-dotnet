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
using Microsoft.AspNetCore.Builder;
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

        return services;
    }

    /// <summary>
    /// Puts the identity resolution in front of the endpoints. It must run before anything that reads an
    /// identity — every page controller does.
    /// </summary>
    public static IApplicationBuilder UseDcaShopIdentity(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<JwtAuthenticationMiddleware>();
    }
}
