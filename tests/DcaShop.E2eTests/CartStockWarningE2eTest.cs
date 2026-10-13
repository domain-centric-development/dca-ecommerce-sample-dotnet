using System.Net;

using DcaShop.E2eTests.Pages;
using DcaShop.Inventory.Application.ReduceStock;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.E2eTests;

/// <summary>
/// The cart page tells a guest which line has less stock than it holds. The test runs a shop of its own: the order it
/// arranges uses up stock of the Hexagon that the suite's shared shop keeps for other tests.
/// </summary>
public sealed class CartStockWarningE2eTest : BaseE2eTest
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    public CartStockWarningE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [RestartingFact(DisplayName = "Short stock line warns")]
    public async Task ALineHoldingMoreThanTheStockLeftWarnsOfInsufficientStock()
    {
        using var shop = ShopOfItsOwn.Start();
        var catalog = await ProductCatalogPage.NavigateToAsync(Page, shop.BaseUrl);
        var hexagon = await catalog.ProductPathAsync(Hexagon);
        var product = await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon);
        Assert.Equal("8 units available", await product.AvailabilityAsync());
        await product.PressAddToCartAsync();
        await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon)).PressAddToCartAsync();
        await shop.AnotherGuestHasBoughtAsync(hexagon, 7);
        Assert.Equal("1 units available", await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon)).AvailabilityAsync());

        var cart = await CartPage.NavigateToAsync(Page, shop.BaseUrl);

        Assert.Equal("2", (await cart.LineAsync(Hexagon)).Quantity);
        Assert.Equal(["Insufficient stock available"], await cart.StockWarningsAsync(Hexagon));
    }

    /// <summary>
    /// A shop beside the suite's own, started for one test with the seeded catalogue and stock, on a port chosen by the
    /// system.
    /// </summary>
    private sealed class ShopOfItsOwn : IDisposable
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
}