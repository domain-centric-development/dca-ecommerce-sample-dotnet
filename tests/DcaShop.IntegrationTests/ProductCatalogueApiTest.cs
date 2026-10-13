using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The catalogue read through the REST API by a client without a token, against the seeded sample catalogue: the
/// listing's order and size, what one entry carries, and a product read by the id the listing gave.
/// </summary>
public sealed class ProductCatalogueApiTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductCatalogueApiTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TheListingReturnsTheSeededRangeInOrdinalNameOrderStartingWithTheEnamelPin()
    {
        var response = await _factory.CreateClient().GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, products.ValueKind);
        Assert.Equal(21, products.GetArrayLength());
        var names = products.EnumerateArray().Select(p => p.GetProperty("name").GetString()!).ToList();
        Assert.Equal(names.Order(StringComparer.Ordinal), names);
        Assert.Equal("\"Bounded Context\" Enamel Pin", names[0]);
    }

    [Fact]
    public async Task AListedProductCarriesItsMasterDataItsPriceAndItsStock()
    {
        var products = await _factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/products");

        var ddd = products.EnumerateArray().Single(p => p.GetProperty("name").GetString() == "Domain-Driven Design");
        Assert.False(string.IsNullOrEmpty(ddd.GetProperty("id").GetString()));
        Assert.Equal("BOOK-001", ddd.GetProperty("sku").GetString());
        Assert.Equal("Domain-Driven Design", ddd.GetProperty("name").GetString());
        Assert.StartsWith("The seminal work by Eric Evans", ddd.GetProperty("description").GetString(), StringComparison.Ordinal);
        Assert.False(string.IsNullOrEmpty(ddd.GetProperty("imageUrl").GetString()));
        Assert.Equal(54.99m, ddd.GetProperty("price").GetDecimal());
        Assert.Equal("EUR", ddd.GetProperty("currency").GetString());
        Assert.Equal("Books", ddd.GetProperty("category").GetString());
        Assert.Equal(20, ddd.GetProperty("stockQuantity").GetInt32());
        Assert.True(ddd.GetProperty("isAvailable").GetBoolean());
    }

    [Fact]
    public async Task OneProductIsReadByTheIdTheListingGave()
    {
        var client = _factory.CreateClient();
        var products = await client.GetFromJsonAsync<JsonElement>("/api/products");
        var id = products.EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == "Hexagonal Architecture Explained")
            .GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/products/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, product.GetProperty("id").GetString());
        Assert.Equal("BOOK-005", product.GetProperty("sku").GetString());
        Assert.Equal(34.99m, product.GetProperty("price").GetDecimal());
        Assert.Equal("EUR", product.GetProperty("currency").GetString());
        Assert.Equal("Books", product.GetProperty("category").GetString());
        Assert.Equal(26, product.GetProperty("stockQuantity").GetInt32());
        Assert.True(product.GetProperty("isAvailable").GetBoolean());
    }
}