using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using DcaShop.Account.Adapter.Incoming.Security;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task AddingAProductAnswersWithTheCartHoldingItsLineAndTotal()
    {
        var client = Bearer(await RegisterAsync("ada@example.com"));
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var domainDrivenDesign = await ProductIdAsync("Domain-Driven Design");

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
        var client = Bearer(await RegisterAsync("ada@example.com"));
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var domainDrivenDesign = await ProductIdAsync("Domain-Driven Design");
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
        var client = Bearer(await RegisterAsync("ada@example.com"));
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        await AddAsync(client, cartId!, await ProductIdAsync("Domain-Driven Design"), 2);
        await AddAsync(client, cartId!, await ProductIdAsync("Team Topologies"), 1);

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

    [Fact]
    public async Task TheStaffListSpellsACartsStatusAsTheSingleCartsAnswerDoes()
    {
        var ada = Bearer(await RegisterAsync("ada@example.com"));
        var opened = await (await ada.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();
        var customerId = opened.GetProperty("customerId").GetString();
        await AddAsync(ada, cartId!, await ProductIdAsync("Domain-Driven Design"), 2);
        var staff = Bearer(StaffToken(SubjectOf(await RegisterAsync("operator@example.com")), "operator@example.com"));

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
        var client = Bearer(await RegisterAsync("ada@example.com"));
        var opened = await (await client.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var cartId = opened.GetProperty("cartId").GetString();

        var response = await client.PostAsJsonAsync($"/api/carts/{cartId}/items", new { productId = "no-such-product", quantity = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Article not available", problem.GetProperty("title").GetString());
        Assert.Equal("There is no article for product no-such-product.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AddingToAnotherCustomersCartFindsNothingAndLeavesItsLinesAsTheyWere()
    {
        var ada = Bearer(await RegisterAsync("ada@example.com"));
        var cartId = await OpenCartHoldingTwoDomainDrivenDesignAsync(ada);
        var grace = Bearer(await RegisterAsync("grace@example.com"));

        var response = await grace.PostAsJsonAsync(
            $"/api/carts/{cartId}/items", new { productId = await ProductIdAsync("Team Topologies"), quantity = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertHoldsOnlyTwoDomainDrivenDesignAsync(ada, cartId);
    }

    [Fact]
    public async Task RemovingFromAnotherCustomersCartFindsNothingAndLeavesItsLinesAsTheyWere()
    {
        var ada = Bearer(await RegisterAsync("ada@example.com"));
        var cartId = await OpenCartHoldingTwoDomainDrivenDesignAsync(ada);
        var grace = Bearer(await RegisterAsync("grace@example.com"));

        var response = await grace.DeleteAsync($"/api/carts/{cartId}/items/{await ProductIdAsync("Domain-Driven Design")}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertHoldsOnlyTwoDomainDrivenDesignAsync(ada, cartId);
    }

    [Fact]
    public async Task AnotherCustomersActiveCartFindsNothing()
    {
        var ada = Bearer(await RegisterAsync("ada@example.com"));
        var opened = await (await ada.PostAsync("/api/carts", Empty())).Content.ReadFromJsonAsync<JsonElement>();
        var adasCustomerId = opened.GetProperty("customerId").GetString();
        var grace = Bearer(await RegisterAsync("grace@example.com"));

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
        await AddAsync(owner, cartId, await ProductIdAsync("Domain-Driven Design"), 2);
        return cartId;
    }

    private async Task AssertHoldsOnlyTwoDomainDrivenDesignAsync(HttpClient owner, string cartId)
    {
        var cart = await owner.GetFromJsonAsync<JsonElement>($"/api/carts/{cartId}");
        var item = Assert.Single(cart.GetProperty("items").EnumerateArray());
        Assert.Equal(await ProductIdAsync("Domain-Driven Design"), item.GetProperty("productId").GetString());
        Assert.Equal(2, item.GetProperty("quantity").GetInt32());
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

    /// <summary>
    /// A staff token for an existing account. No registration path hands the role out, so the test mints the
    /// token the way an operator provisioning tool would.
    /// </summary>
    private string StaffToken(string userId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<JwtTokenService>().GenerateRegisteredToken(
            UserId.Of(userId),
            email,
            new HashSet<string> { IIdentityProvider.IIdentity.RoleCustomer, IIdentityProvider.IIdentity.RoleStaff });
    }

    private static string SubjectOf(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return json.RootElement.GetProperty("sub").GetString()!;
    }

    private static StringContent Empty() => new(string.Empty, Encoding.UTF8, "application/json");
}