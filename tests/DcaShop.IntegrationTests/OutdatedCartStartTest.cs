using System.Net;

using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Checkout.Application.Session.StartCheckout;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Product.Application.GetAllProducts;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A checkout started from a cart whose line ran short of stock since the cart page was opened is refused with the
/// lines that cannot be bought, and the checkout page answers that refusal with the cart and its error. A host of its
/// own: the stock of the Hexagon goes down here.
/// </summary>
public sealed class OutdatedCartStartTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    private readonly WebApplicationFactory<Program> _factory;

    public OutdatedCartStartTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task StartingFromACartWhoseLineRanShortOfStockIsRefusedNamingThatLine()
    {
        var guest = $"guest-{Guid.NewGuid()}";
        using var scope = _factory.Services.CreateScope();
        var products = scope.ServiceProvider.GetRequiredService<IGetAllProductsInputPort>();
        var hexagon = (await products.ExecuteAsync(new GetAllProductsQuery())).Products.Single(p => p.Name.Value == Hexagon);
        Assert.Equal(8, hexagon.Article.AvailableStock);
        var cart = await scope.ServiceProvider.GetRequiredService<IGetOrCreateActiveCartInputPort>().ExecuteAsync(new GetOrCreateActiveCartCommand(guest));
        await scope.ServiceProvider.GetRequiredService<IAddItemToCartInputPort>()
            .ExecuteAsync(new AddItemToCartCommand(cart.CartId, guest, hexagon.ProductId.Value, 5));
        await GuestOrders.AnotherGuestHasBoughtAsync(_factory, hexagon.ProductId.Value, 5);
        var start = scope.ServiceProvider.GetRequiredService<IStartCheckoutInputPort>();

        var refused = await Assert.ThrowsAsync<CheckoutItemsUnavailableException>(() =>
            start.ExecuteAsync(new StartCheckoutCommand(cart.CartId, guest)));

        Assert.Equal([hexagon.ProductId], refused.ProductIds);
        using var shop = ShopWhoseStartRefusesWith(refused);
        var visitor = GuestOrders.Guest(shop);
        var productPage = await visitor.GetStringAsync(await AvailabilityPages.ProductPathAsync(visitor, Hexagon));
        var started = await visitor.PostAsync("/checkout/start", GuestOrders.Form(productPage, ("cartId", cart.CartId.ToString())));
        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);
        Assert.Equal("/cart", started.Headers.Location!.ToString());
        Assert.Equal(
            "Cannot start checkout, 1 item(s) unavailable or out of stock.",
            AvailabilityPages.Text(await visitor.GetStringAsync(started.Headers.Location), @"data-test=""cart-error-message"">([^<]*)</div>"));
    }

    /// <summary>The shop with a start that answers every command with <paramref name="refusal"/> — the page's translation alone.</summary>
    private WebApplicationFactory<Program> ShopWhoseStartRefusesWith(CheckoutItemsUnavailableException refusal) =>
        _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(s => s.AddSingleton<IStartCheckoutInputPort>(new RefusingStart(refusal))));

    private sealed class RefusingStart(CheckoutItemsUnavailableException refusal) : IStartCheckoutInputPort
    {
        public Task<StartCheckoutResult> ExecuteAsync(StartCheckoutCommand command, CancellationToken cancellationToken = default) =>
            throw refusal;
    }
}