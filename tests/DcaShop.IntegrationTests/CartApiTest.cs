

using System.Net;
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
        var client = await _factory.CustomerClientAsync("ada@example.com");

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
        var client = await _factory.CustomerClientAsync("ada@example.com");
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
        var client = await _factory.CustomerClientAsync("ada@example.com");
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
    public async Task ReadingTheActiveCartLeavesNoCartBehindSoOpeningOneIsCreated()
    {
        var (token, customerId) = await _factory.RegisterThroughTheApiAsync("grace@example.com");
        var grace = _factory.Bearer(token);
        await grace.GetAsync($"/api/carts/customer/{customerId}/active");

        var response = await grace.PostAsync("/api/carts", Empty());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, cart.GetProperty("items").GetArrayLength());
        Assert.Equal("ACTIVE", cart.GetProperty("status").GetString());
        Assert.Equal(0, cart.GetProperty("itemCount").GetInt32());
    }

    [Fact]
    public async Task AddingAProductAnswersWithTheCartHoldingItsLineAndTotal()
    {
        var client = await _factory.CustomerClientAsync("ada@example.com");
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var domainDrivenDesign = await _factory.ProductIdAsync("Domain-Driven Design");

        var response = await client.PostAsJsonAsync($"/api/carts/{cartId}/items", new { productId = domainDrivenDesign, quantity = 2 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<JsonElement>();
        var item = Assert.Single(cart.GetProperty("items").EnumerateArray());
        Assert.Equal(domainDrivenDesign, item.GetProperty("productId").GetString());
        Assert.Equal(2, item.GetProperty("quantity").GetInt32());
        Assert.Equal(109.98m, cart.GetProperty("total").GetDecimal());
        Assert.Equal("EUR", cart.GetProperty("currency").GetString());
        Assert.Equal(1, cart.GetProperty("itemCount").GetInt32());
    }

    [Fact]
    public async Task AddingAProductTheCartHoldsRaisesItsLineInsteadOfAddingOne()
    {
        var client = await _factory.CustomerClientAsync("ada@example.com");
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var domainDrivenDesign = await _factory.ProductIdAsync("Domain-Driven Design");
        await AddAsync(client, cartId!, domainDrivenDesign, 2);

        var response = await client.PostAsJsonAsync($"/api/carts/{cartId}/items", new { productId = domainDrivenDesign, quantity = 1 });

        var cart = await response.Content.ReadFromJsonAsync<JsonElement>();
        var item = Assert.Single(cart.GetProperty("items").EnumerateArray());
        Assert.Equal(3, item.GetProperty("quantity").GetInt32());
        Assert.Equal(164.97m, cart.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task ReadingTheOwnCartAnswersItsLinesTotalAndLineCount()
    {
        var client = await _factory.CustomerClientAsync("ada@example.com");
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        await AddAsync(client, cartId!, await _factory.ProductIdAsync("Domain-Driven Design"), 2);
        await AddAsync(client, cartId!, await _factory.ProductIdAsync("Team Topologies"), 1);

        var response = await client.GetAsync($"/api/carts/{cartId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, cart.GetProperty("items").GetArrayLength());
        Assert.Equal(139.97m, cart.GetProperty("total").GetDecimal());
        Assert.Equal("EUR", cart.GetProperty("currency").GetString());
        Assert.Equal(2, cart.GetProperty("itemCount").GetInt32());
    }

    [Fact]
    public async Task RemovingAProductByItsIdTakesItsWholeLineOut()
    {
        var client = await _factory.CustomerClientAsync("ada@example.com");
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var domainDrivenDesign = await _factory.ProductIdAsync("Domain-Driven Design");
        var teamTopologies = await _factory.ProductIdAsync("Team Topologies");
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

    [Fact]
    public async Task TheStaffListSpellsACartsStatusAsTheSingleCartsAnswerDoes()
    {
        var ada = await _factory.CustomerClientAsync("ada@example.com");
        var opened = await (await ada.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var customerId = opened.GetProperty("customerId").GetString();
        await AddAsync(ada, cartId!, await _factory.ProductIdAsync("Domain-Driven Design"), 2);
        var staff = await _factory.StaffClientAsync("operator@example.com");

        var response = await staff.GetAsync("/api/carts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var carts = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("carts");
        var cart = Assert.Single(carts.EnumerateArray(), c => c.GetProperty("cartId").GetString() == cartId);
        Assert.Equal(customerId, cart.GetProperty("customerId").GetString());
        Assert.Equal("ACTIVE", cart.GetProperty("status").GetString());
        Assert.Equal(1, cart.GetProperty("itemCount").GetInt32());
        Assert.Equal(109.98m, cart.GetProperty("total").GetDecimal());
        Assert.Equal("EUR", cart.GetProperty("currency").GetString());
    }

    [Fact]
    public async Task AProductIdNoProductHasIsAnArticleThatIsNotAvailable()
    {
        var client = await _factory.CustomerClientAsync("ada@example.com");
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();

        var response = await client.PostAsJsonAsync($"/api/carts/{cartId}/items", new { productId = "00000000-0000-4000-8000-000000000001", quantity = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Article not available", problem.GetProperty("title").GetString());
        Assert.Equal("There is no article for product 00000000-0000-4000-8000-000000000001.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AddingMoreThanIsInStockIsAConflictAndLeavesTheCartEmpty()
    {
        var client = await _factory.CustomerClientAsync("ada@example.com");
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var hexagon = await _factory.ProductIdAsync("Wooden Hexagon Desk Model");

        var response = await client.PostAsJsonAsync($"/api/carts/{cartId}/items", new { productId = hexagon, quantity = 9 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Not enough stock", problem.GetProperty("title").GetString());
        Assert.Equal($"Requested 9 of product {hexagon}, more than is in stock.", problem.GetProperty("detail").GetString());
        var cart = await client.GetFromJsonAsync<JsonElement>($"/api/carts/{cartId}");
        Assert.Equal(0, cart.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task AddingExactlyWhatIsInStockIsAccepted()
    {
        var client = await _factory.CustomerClientAsync("ada@example.com");
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var hexagon = await _factory.ProductIdAsync("Wooden Hexagon Desk Model");

        var response = await client.PostAsJsonAsync($"/api/carts/{cartId}/items", new { productId = hexagon, quantity = 8 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<JsonElement>();
        var item = Assert.Single(cart.GetProperty("items").EnumerateArray());
        Assert.Equal(hexagon, item.GetProperty("productId").GetString());
        Assert.Equal(8, item.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task AQuantityOfZeroIsABadRequestAndLeavesTheCartEmpty()
    {
        var client = await _factory.CustomerClientAsync("ada@example.com");
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();

        var response = await client.PostAsJsonAsync(
            $"/api/carts/{cartId}/items", new { productId = await _factory.ProductIdAsync("Domain-Driven Design"), quantity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var cart = await client.GetFromJsonAsync<JsonElement>($"/api/carts/{cartId}");
        Assert.Equal(0, cart.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task RemovingAProductTheCartDoesNotHoldIsNotFound()
    {
        var client = await _factory.CustomerClientAsync("ada@example.com");
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        await AddAsync(client, cartId!, await _factory.ProductIdAsync("Domain-Driven Design"), 1);
        var teamTopologies = await _factory.ProductIdAsync("Team Topologies");

        var response = await client.DeleteAsync($"/api/carts/{cartId}/items/{teamTopologies}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Position not in cart", problem.GetProperty("title").GetString());
        Assert.Equal($"The cart holds no position for {teamTopologies}.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AddingToAnotherCustomersCartFindsNothingAndLeavesItsLinesAsTheyWere()
    {
        var ada = await _factory.CustomerClientAsync("ada@example.com");
        var cartId = await OpenCartHoldingTwoDomainDrivenDesignAsync(ada);
        var grace = await _factory.CustomerClientAsync("grace@example.com");

        var response = await grace.PostAsJsonAsync(
            $"/api/carts/{cartId}/items", new { productId = await _factory.ProductIdAsync("Team Topologies"), quantity = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertHoldsOnlyTwoDomainDrivenDesignAsync(ada, cartId);
    }

    [Fact]
    public async Task RemovingFromAnotherCustomersCartFindsNothingAndLeavesItsLinesAsTheyWere()
    {
        var ada = await _factory.CustomerClientAsync("ada@example.com");
        var cartId = await OpenCartHoldingTwoDomainDrivenDesignAsync(ada);
        var grace = await _factory.CustomerClientAsync("grace@example.com");

        var response = await grace.DeleteAsync($"/api/carts/{cartId}/items/{await _factory.ProductIdAsync("Domain-Driven Design")}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertHoldsOnlyTwoDomainDrivenDesignAsync(ada, cartId);
    }

    [Fact]
    public async Task AnotherCustomersActiveCartFindsNothing()
    {
        var ada = await _factory.CustomerClientAsync("ada@example.com");
        var opened = await (await ada.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var adasCustomerId = opened.GetProperty("customerId").GetString();
        var grace = await _factory.CustomerClientAsync("grace@example.com");

        var response = await grace.GetAsync($"/api/carts/customer/{adasCustomerId}/active");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListingCartsWithoutATokenIsChallengedForABearerToken()
    {
        var response = await _factory.CreateClient().GetAsync("/api/carts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task TwoCallsWithoutATokenOpenCartsForTwoDifferentCustomers()
    {
        var first = await _factory.CreateClient().PostAsync("/api/carts", Empty());
        var second = await _factory.CreateClient().PostAsync("/api/carts", Empty());

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var firstCustomerId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("customerId").GetString();
        var secondCustomerId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("customerId").GetString();
        Assert.NotEqual(firstCustomerId, secondCustomerId);
    }

    private async Task<string> OpenCartHoldingTwoDomainDrivenDesignAsync(HttpClient owner)
    {
        var opened = await (await owner.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString()!;
        await AddAsync(owner, cartId, await _factory.ProductIdAsync("Domain-Driven Design"), 2);
        return cartId;
    }

    private async Task AssertHoldsOnlyTwoDomainDrivenDesignAsync(HttpClient owner, string cartId)
    {
        var cart = await owner.GetFromJsonAsync<JsonElement>($"/api/carts/{cartId}");
        var item = Assert.Single(cart.GetProperty("items").EnumerateArray());
        Assert.Equal(await _factory.ProductIdAsync("Domain-Driven Design"), item.GetProperty("productId").GetString());
        Assert.Equal(2, item.GetProperty("quantity").GetInt32());
    }

    private async Task AddAsync(HttpClient client, string cartId, string productId, int quantity)
    {
        var response = await client.PostAsJsonAsync($"/api/carts/{cartId}/items", new { productId, quantity });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static StringContent Empty() => new(string.Empty, Encoding.UTF8, "application/json");
}