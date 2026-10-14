using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using DcaShop.Account.Adapter.Incoming.Security;
using DcaShop.Product.Application.GetAllProducts;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A member of staff creating a product whose price or initial stock sits on a bound: what the shop answers, and
/// what the catalogue holds afterwards. Each test starts a shop of its own, so the seeded catalogue it counts is not
/// touched by the products other tests create.
/// </summary>
public sealed class ProductPriceAndStockRulesTest
{
    private static readonly Regex CardTitle = new(@"data-test=""product-card-title"">([^<]*)<", RegexOptions.Compiled);

    [Fact]
    public async Task APriceOfZeroIsRefusedAndTheCatalogueKeepsItsProducts()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await StaffClientAsync(shop, "free-mug@example.com");

        var response = await staff.PostAsync("/api/products", Json(
            """{"sku":"FREE-001","name":"Free Mug","description":"","imageUrl":"","price":0,"category":"Desk & Office","stock":5}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(21, await ProductCountAsync(shop));
    }

    [Fact]
    public async Task APriceThatRoundsToZeroIsAnUnacceptableValue()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await StaffClientAsync(shop, "tiny-mug@example.com");

        var response = await staff.PostAsync("/api/products", Json(
            """{"sku":"TINY-001","name":"Tiny Mug","description":"","imageUrl":"","price":0.001,"category":"Desk & Office","stock":5}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task TheHighestPriceIsAcceptedAndShownOnTheCard()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await StaffClientAsync(shop, "priceless@example.com");

        var response = await staff.PostAsync("/api/products", Json(
            """{"sku":"MAX-001","name":"Priceless Model","description":"","imageUrl":"","price":999999999999.99,"category":"Modeling","stock":1}"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var card = await CardOncePricedAsync(shop, "Priceless Model");
        Assert.Equal("999999999999.99 EUR", PriceOf(card));
    }

    [Fact]
    public async Task APriceAboveTheHighestIsAnUnacceptableValue()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await StaffClientAsync(shop, "too-precious@example.com");

        var response = await staff.PostAsync("/api/products", Json(
            """{"sku":"MAX-002","name":"Too Precious Model","description":"","imageUrl":"","price":1000000000000.00,"category":"Modeling","stock":1}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task AnInitialStockOfZeroIsAcceptedAndTheCardShowsItOutOfStock()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await StaffClientAsync(shop, "sold-out-pin@example.com");

        var response = await staff.PostAsync("/api/products", Json(
            """{"sku":"PIN-002","name":"Sold Out Pin","description":"","imageUrl":"","price":12.99,"category":"Stickers & Pins","stock":0}"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var card = await CardOncePricedAsync(shop, "Sold Out Pin");
        Assert.Equal("12.99 EUR", PriceOf(card));
        Assert.Equal("Out of Stock", Text(card, @"class=""product-card__badge[^""]*"">([^<]*)</span>"));
    }

    [Fact]
    public async Task ANegativeInitialStockIsRefusedAndTheCatalogueKeepsItsProducts()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await StaffClientAsync(shop, "owed-pin@example.com");

        var response = await staff.PostAsync("/api/products", Json(
            """{"sku":"PIN-003","name":"Owed Pin","description":"","imageUrl":"","price":12.99,"category":"Stickers & Pins","stock":-1}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(21, await ProductCountAsync(shop));
    }

    private static async Task<int> ProductCountAsync(WebApplicationFactory<Program> shop)
    {
        using var scope = shop.Services.CreateScope();
        var catalogue = await scope.ServiceProvider.GetRequiredService<IGetAllProductsInputPort>().ExecuteAsync(new GetAllProductsQuery());
        return catalogue.Products.Count;
    }

    /// <summary>The card of <paramref name="name"/> once Pricing has taken the price over from the creation.</summary>
    private static async Task<string> CardOncePricedAsync(WebApplicationFactory<Program> shop, string name)
    {
        var client = shop.CreateClient();
        for (var i = 0; i < 50; i++)
        {
            if (CardOf(await client.GetStringAsync("/products"), name) is { } card && PriceOf(card) != "0.00 EUR")
            {
                return card;
            }

            await Task.Delay(100);
        }

        Assert.Fail($"the card of {name} shows no price within 5 seconds");
        throw new InvalidOperationException("unreachable");
    }

    private static string? CardOf(string catalogue, string name) =>
        catalogue.Split("data-test=\"product-card\"").Skip(1).SingleOrDefault(card => CardTitle.Match(card) is { Success: true } title
            && WebUtility.HtmlDecode(title.Groups[1].Value).Trim() == name);

    private static string PriceOf(string card) => Text(card, @"class=""product-card__price"">([^<]*)</div>");

    private static string Text(string markup, string pattern)
    {
        var match = Regex.Match(markup, pattern);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
    }

    private static async Task<HttpClient> StaffClientAsync(WebApplicationFactory<Program> shop, string email)
    {
        var registered = await shop.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123",
            firstName = "Ada",
            lastName = "Lovelace",
            dateOfBirth = "1815-12-10",
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var token = (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;

        using var scope = shop.Services.CreateScope();
        var staffToken = scope.ServiceProvider.GetRequiredService<JwtTokenService>().GenerateRegisteredToken(
            UserId.Of(SubjectOf(token)),
            email,
            new HashSet<Role> { Role.Customer, Role.Staff });
        var client = shop.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        return client;
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static string SubjectOf(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return json.RootElement.GetProperty("sub").GetString()!;
    }
}