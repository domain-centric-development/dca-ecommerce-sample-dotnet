using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.E2eTests;

/// <summary>
/// The shop the suite drives as it is after a restart: a second shop beside it, started with the same configuration
/// and therefore the same signing secret, but with every store empty. A browser keeps its cookies across the two,
/// because cookies tell hosts apart, not ports.
/// </summary>
public sealed class RestartedShop : IDisposable
{
    private readonly WebApplicationFactory<Program> _shop;

    private RestartedShop(WebApplicationFactory<Program> shop, string baseUrl)
    {
        _shop = shop;
        BaseUrl = baseUrl;
    }

    public string BaseUrl { get; }

    public static RestartedShop Start()
    {
        // A port of its own, chosen by the system, beside the suite's shop.
        var shop = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("Checkout:PaymentProvider:BaseUrl", PaymentProviderStub.Url)
            .ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0)));
        shop.UseKestrel();
        shop.StartServer();

        var address = shop.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new RestartedShop(shop, $"http://localhost:{new Uri(address).Port}");
    }

    public void Dispose() => _shop.Dispose();
}

/// <summary>
/// An E2E test that restarts the shop. Only a shop the suite started itself can be restarted; against the one
/// <c>E2E_BASE_URL</c> names it skips instead of failing.
/// </summary>
public sealed class RestartingFactAttribute : FactAttribute
{
    public RestartingFactAttribute()
    {
        if (ShopUnderTest.StartedElsewhere)
        {
            Skip = "restarts the shop, which only a shop the suite started itself allows (no E2E_BASE_URL)";
        }
    }
}