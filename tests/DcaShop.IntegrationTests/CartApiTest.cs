using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Driving a cart through the API the way a partner application does: one open cart per customer, its status
/// spelled as the other answers spell it, and a line removed by the product it holds.
/// </summary>
/// <remarks>
/// Each test starts its own shop: carts and accounts live in memory, and every test registers "ada@example.com".
/// </remarks>
public sealed class CartApiTest : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task OpeningACartAnswersCreatedWithAnEmptyActiveCartOfTheCaller()
    {
        var client = Bearer(await RegisterAsync("ada@example.com"));

        var response = await client.PostAsync("/api/carts", Empty());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(cart.GetProperty("cartId").GetString()));
        Assert.False(string.IsNullOrEmpty(cart.GetProperty("customerId").GetString()));
        Assert.Equal(0, cart.GetProperty("items").GetArrayLength());
        Assert.Equal("ACTIVE", cart.GetProperty("status").GetString());
        Assert.Equal(0, cart.GetProperty("itemCount").GetInt32());
    }

    [Fact]
    public async Task OpeningASecondCartWhileOneIsOpenIsAConflict()
    {
        var client = Bearer(await RegisterAsync("ada@example.com"));
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsync("/api/carts", Empty())).StatusCode);

        var response = await client.PostAsync("/api/carts", Empty());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Active cart already exists", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task TheActiveCartIsReadByTheCustomerIdTheOpeningReturned()
    {
        var client = Bearer(await RegisterAsync("ada@example.com"));
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var customerId = opened.GetProperty("customerId").GetString();

        var response = await client.GetAsync($"/api/carts/customer/{customerId}/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(cartId, cart.GetProperty("cartId").GetString());
        Assert.Equal("ACTIVE", cart.GetProperty("status").GetString());
    }

    [Fact]
    public async Task RemovingAProductByItsIdTakesItsWholeLineOut()
    {
        var client = Bearer(await RegisterAsync("ada@example.com"));
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var domainDrivenDesign = await ProductIdAsync("Domain-Driven Design");
        var teamTopologies = await ProductIdAsync("Team Topologies");
        await AddAsync(client, cartId!, domainDrivenDesign, 3);
        await AddAsync(client, cartId!, teamTopologies, 1);

        var response = await client.DeleteAsync($"/api/carts/{cartId}/items/{domainDrivenDesign}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<JsonElement>();
        var item = Assert.Single(cart.GetProperty("items").EnumerateArray());
        Assert.Equal(teamTopologies, item.GetProperty("productId").GetString());
        Assert.Equal(1, item.GetProperty("quantity").GetInt32());
        Assert.Equal(1, cart.GetProperty("itemCount").GetInt32());
    }

    private async Task AddAsync(HttpClient client, string cartId, string productId, int quantity)
    {
        var response = await client.PostAsJsonAsync($"/api/carts/{cartId}/items", new { productId, quantity });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> ProductIdAsync(string name)
    {
        var products = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/products");
        return products.EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == name)
            .GetProperty("id").GetString()!;
    }

    private HttpClient Bearer(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> RegisterAsync(string email)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123",
            firstName = "Ada",
            lastName = "Lovelace",
            dateOfBirth = "1815-12-10",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private static StringContent Empty() => new(string.Empty, Encoding.UTF8, "application/json");
}