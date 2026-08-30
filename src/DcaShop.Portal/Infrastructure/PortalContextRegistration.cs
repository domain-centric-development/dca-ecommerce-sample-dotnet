using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.Portal.Infrastructure;

/// <summary>
/// Wires the Portal context. It registers nothing today: the context is a UI shell whose controllers are
/// discovered as an application part, and it owns no port and no domain service.
/// </summary>
public static class PortalContextRegistration
{
    public static IServiceCollection AddPortalContext(this IServiceCollection services) => services;
}
