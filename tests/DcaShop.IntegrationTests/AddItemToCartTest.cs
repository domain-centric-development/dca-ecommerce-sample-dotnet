using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetCartById;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Product.Application.GetAllProducts;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Adding a product to a cart through the input port in the wired application: one line per product, its quantity
/// raised when the product is added again.
/// </summary>
public sealed class AddItemToCartTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AddItemToCartTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task AddingTheSameProductAgainRaisesTheQuantityOfItsLine()
    {
        var customer = $"guest-{Guid.NewGuid()}";
        var cartId = await CartWithOneUnitOfAsync(customer, "Domain-Driven Design");

        await AddOneUnitAsync(cartId, customer, "Domain-Driven Design");

        Assert.Equal(["Domain-Driven Design x2"], await LinesAsync(cartId, customer));
    }

    [Fact]
    public async Task AnotherProductGetsALineOfItsOwn()
    {
        var customer = $"guest-{Guid.NewGuid()}";
        var cartId = await CartWithOneUnitOfAsync(customer, "Domain-Driven Design");

        await AddOneUnitAsync(cartId, customer, "Clean Architecture");

        Assert.Equal(["Domain-Driven Design x1", "Clean Architecture x1"], await LinesAsync(cartId, customer));
    }

    private async Task<Guid> CartWithOneUnitOfAsync(string customer, string productName)
    {
        using var scope = _factory.Services.CreateScope();
        var cart = await scope.ServiceProvider.GetRequiredService<IGetOrCreateActiveCartInputPort>()
            .ExecuteAsync(new GetOrCreateActiveCartCommand(customer));
        await AddOneUnitAsync(cart.CartId, customer, productName);
        return cart.CartId;
    }

    private async Task AddOneUnitAsync(Guid cartId, string customer, string productName)
    {
        using var scope = _factory.Services.CreateScope();
        var products = await scope.ServiceProvider.GetRequiredService<IGetAllProductsInputPort>().ExecuteAsync(new GetAllProductsQuery());
        var product = products.Products.Single(p => p.Name.Value == productName);
        await scope.ServiceProvider.GetRequiredService<IAddItemToCartInputPort>()
            .ExecuteAsync(new AddItemToCartCommand(cartId, customer, product.ProductId.Value.ToString(), 1));
    }

    /// <summary>The cart's lines as <c>name xquantity</c>, in the order the cart holds them.</summary>
    private async Task<IReadOnlyList<string>> LinesAsync(Guid cartId, string customer)
    {
        using var scope = _factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IGetCartByIdInputPort>()
            .ExecuteAsync(new GetCartByIdQuery(cartId, customer));
        Assert.NotNull(result.Cart);
        return result.Cart.Items.Select(i => $"{i.Article.Name} x{i.Quantity.Value}").ToList();
    }
}