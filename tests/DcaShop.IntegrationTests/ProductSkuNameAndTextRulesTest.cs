using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using DcaShop.Account.Adapter.Incoming.Security;
using DcaShop.Product.Application.GetAllProducts;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A member of staff creating a product whose stock keeping unit, name, description or image URL the catalogue
/// cannot take: what the shop answers, and what the catalogue holds afterwards. Each test starts a shop of its own,
/// so the seeded catalogue it counts is not touched by the products other tests create.
/// </summary>
public sealed class ProductSkuNameAndTextRulesTest
{
    [Fact]
    public async Task ALowerCaseSkuIsAnUnacceptableValue()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await shop.StaffClientAsync("lower-case-mug@example.com");

        var response = await staff.PostAsJsonAsync("/api/products", new
        {
            sku = "mug-002",
            name = "Aggregate Mug",
            description = "",
            imageUrl = "",
            price = 14.99m,
            category = "Desk & Office",
            stock = 12,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task ANameOf256CharactersIsAnUnacceptableValue()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await shop.StaffClientAsync("long-name@example.com");

        var response = await staff.PostAsJsonAsync("/api/products", ProductWith("LONG-001", new string('A', 256)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task ANameOf255CharactersIsAccepted()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await shop.StaffClientAsync("longest-name@example.com");

        var response = await staff.PostAsJsonAsync("/api/products", ProductWith("LONG-002", new string('A', 255)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ABlankNameIsRefusedAndTheCatalogueKeepsItsProducts()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await shop.StaffClientAsync("blank-name@example.com");

        var response = await staff.PostAsJsonAsync("/api/products", ProductWith("BLANK-001", "   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(21, await ProductCountAsync(shop));
    }

    [Fact]
    public async Task ADescriptionOf2001CharactersIsAnUnacceptableValue()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await shop.StaffClientAsync("long-story@example.com");

        var response = await staff.PostAsJsonAsync("/api/products", new
        {
            sku = "DESC-001",
            name = "Long Story",
            description = new string('A', 2001),
            imageUrl = "",
            price = 9.99m,
            category = "Books",
            stock = 1,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task AnImageUrlOf2001CharactersIsAnUnacceptableValue()
    {
        using var shop = new WebApplicationFactory<Program>();
        var staff = await shop.StaffClientAsync("big-picture@example.com");
        var imageUrl = "https://images.example.com/" + new string('a', 2001 - "https://images.example.com/".Length);

        var response = await staff.PostAsJsonAsync("/api/products", new
        {
            sku = "IMG-001",
            name = "Big Picture",
            description = "",
            imageUrl,
            price = 9.99m,
            category = "Books",
            stock = 1,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    private static object ProductWith(string sku, string name) => new
    {
        sku,
        name,
        description = "",
        imageUrl = "",
        price = 9.99m,
        category = "Books",
        stock = 1,
    };

    private static async Task<int> ProductCountAsync(WebApplicationFactory<Program> shop)
    {
        using var scope = shop.Services.CreateScope();
        var catalogue = await scope.ServiceProvider.GetRequiredService<IGetAllProductsInputPort>().ExecuteAsync(new GetAllProductsQuery());
        return catalogue.Products.Count;
    }

}