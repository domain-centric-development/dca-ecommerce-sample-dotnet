using System.Net;

using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.StartCheckout;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Product.Application.GetAllProducts;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A checkout started from a cart whose line ran short of stock since the cart page was opened: the use case once,
/// through its input port, refuses it with the lines that cannot be bought and opens no session; the start page,
/// against a stubbed input port, answers that refusal with the cart and its error. A host of its own: the stock of
/// the Hexagon goes down here.
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
        var session = await scope.ServiceProvider.GetRequiredService<IGetActiveCheckoutSessionInputPort>()
            .ExecuteAsync(new GetActiveCheckoutSessionQuery(guest));
        Assert.False(session.Found);
    }

    [Fact]
    public async Task ARefusedStartForUnavailableLinesOpensTheCartWithItsError()
    {
        var refusal = new CheckoutItemsUnavailableException([ProductId.Generate()]);
        var start = new RefusingStart(refusal);
        using var shop = ShopWhoseStartIs(start);
        var visitor = GuestOrders.Guest(shop);
        var productPage = await visitor.GetStringAsync(await AvailabilityPages.ProductPathAsync(visitor, Hexagon));
        var cartId = Guid.NewGuid();

        var started = await visitor.PostAsync("/checkout/start", GuestOrders.Form(productPage, ("cartId", cartId.ToString())));

        Assert.Equal(cartId, Assert.Single(start.Commands).CartId);
        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);
        Assert.Equal("/cart", started.Headers.Location!.ToString());
        Assert.Equal(
            "Cannot start checkout, 1 item(s) unavailable or out of stock.",
            AvailabilityPages.Text(await visitor.GetStringAsync(started.Headers.Location), @"data-test=""cart-error-message"">([^<]*)</div>"));
    }

    /// <summary>The shop with <paramref name="start"/> in place of the use case — the page's translation alone.</summary>
    private WebApplicationFactory<Program> ShopWhoseStartIs(IStartCheckoutInputPort start) =>
        _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(s => s.AddSingleton(start)));

    /// <summary>A start that answers every command with <paramref name="refusal"/> and keeps the commands it was given.</summary>
    private sealed class RefusingStart(CheckoutItemsUnavailableException refusal) : IStartCheckoutInputPort
    {
        public List<StartCheckoutCommand> Commands { get; } = [];

        public Task<StartCheckoutResult> ExecuteAsync(StartCheckoutCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            throw refusal;
        }
    }
}