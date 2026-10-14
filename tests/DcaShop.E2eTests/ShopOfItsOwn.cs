using System.Net;

using DcaShop.Inventory.Application.ReduceStock;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.E2eTests;

/// <summary>
/// A shop beside the suite's own, started for one test with the seeded catalogue and stock, on a port chosen by the
/// system — for a test that uses up stock the suite's shared shop keeps for other tests.
/// </summary>
public sealed class ShopOfItsOwn : IDisposable
{
    private readonly WebApplicationFactory<Program> _shop;

    private ShopOfItsOwn(WebApplicationFactory<Program> shop, string baseUrl)
    {
        _shop = shop;
        BaseUrl = baseUrl;
    }

    public string BaseUrl { get; }

    public static ShopOfItsOwn Start()
    {
        var shop = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("Checkout:PaymentProvider:BaseUrl", PaymentProviderStub.Url)
            .UseSetting("Checkout:PaymentProvider:Timeout", PaymentProviderStub.Timeout)
            .ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0)));
        shop.UseKestrel();
        shop.StartServer();

        var address = shop.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new ShopOfItsOwn(shop, $"http://localhost:{new Uri(address).Port}");
    }

    /// <summary>
    /// What another guest's placed order does to the stock: the reduction its confirmation triggers in Inventory.
    /// </summary>
    public async Task AnotherGuestHasBoughtAsync(string productPath, int quantity)
    {
        using var scope = _shop.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IReduceStockInputPort>()
            .ExecuteAsync(new ReduceStockCommand([new ReduceStockCommand.Line(Guid.Parse(productPath["/products/".Length..]), quantity)], Guid.NewGuid()));
    }

    public void Dispose() => _shop.Dispose();
}