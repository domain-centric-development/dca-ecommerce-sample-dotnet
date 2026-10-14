using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using DcaShop.Cart.Application.Shopping.RemoveItemFromCart;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The cart resource's translation of a removal: what it answers for a product id it cannot even read. The removal
/// behind it is a stand-in that records what it was asked; the cart is filled and read through the real use cases.
/// </summary>
public sealed class CartItemRemovalResourceTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly RecordingRemoveItemFromCart _removeItemFromCart = new();

    public CartItemRemovalResourceTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(s => s.AddSingleton<IRemoveItemFromCartInputPort>(_removeItemFromCart)));
    }

    [Fact]
    public async Task RemovingAProductIdThatIsNotAUuidIsABadRequestAndLeavesTheLine()
    {
        var ada = await _factory.CustomerClientAsync($"ada-{Guid.NewGuid():N}@example.com");
        var opened = await (await ada.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var domainDrivenDesign = await _factory.ProductIdAsync("Domain-Driven Design");
        Assert.Equal(
            HttpStatusCode.OK,
            (await ada.PostAsJsonAsync($"/api/carts/{cartId}/items", new { productId = domainDrivenDesign, quantity = 1 })).StatusCode);

        var response = await ada.DeleteAsync($"/api/carts/{cartId}/items/no-such-product");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
        Assert.Empty(_removeItemFromCart.Commands);
        var cart = await ada.GetFromJsonAsync<JsonElement>($"/api/carts/{cartId}");
        var item = Assert.Single(cart.GetProperty("items").EnumerateArray());
        Assert.Equal(domainDrivenDesign, item.GetProperty("productId").GetString());
        Assert.Equal(1, item.GetProperty("quantity").GetInt32());
    }

    private static StringContent Empty() => new(string.Empty, Encoding.UTF8, "application/json");

    /// <summary>Records every removal it is asked for and serves none: the resource must not get this far.</summary>
    private sealed class RecordingRemoveItemFromCart : IRemoveItemFromCartInputPort
    {
        public List<RemoveItemFromCartCommand> Commands { get; } = [];

        public Task<RemoveItemFromCartResult> ExecuteAsync(RemoveItemFromCartCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            throw new NotSupportedException("A removal this test never expects to reach the use case.");
        }
    }
}