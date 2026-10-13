using System.Net;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.E2eTests;

/// <summary>
/// A shop beside the suite's own, started for one test with the suite's configuration and the seeded catalogue and
/// stock, on a port chosen by the system. A test that needs a service the shop does not have adds it.
/// </summary>
public sealed class SideShop : IDisposable
{
    private readonly WebApplicationFactory<Program> _shop;

    private SideShop(WebApplicationFactory<Program> shop, string baseUrl)
    {
        _shop = shop;
        BaseUrl = baseUrl;
    }

    public string BaseUrl { get; }

    /// <summary>The shop's services, for what a test arranges behind the browser's back.</summary>
    public IServiceProvider Services => _shop.Services;

    public static SideShop Start(Action<IServiceCollection>? testServices = null)
    {
        var shop = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("Checkout:PaymentProvider:BaseUrl", PaymentProviderStub.Url)
            .ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0))
            .ConfigureTestServices(testServices ?? (_ => { })));
        shop.UseKestrel();
        shop.StartServer();

        var address = shop.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new SideShop(shop, $"http://localhost:{new Uri(address).Port}");
    }

    public void Dispose() => _shop.Dispose();
}