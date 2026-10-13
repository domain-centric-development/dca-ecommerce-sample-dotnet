using System.Text.Json;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DcaShop.E2eTests;

/// <summary>
/// The shop's MCP server as an AI assistant meets it: connected over streamable HTTP at <c>/mcp</c>, without any
/// credentials, it says who it is and offers the catalogue as two tools. No browser — an assistant has none.
/// </summary>
public sealed class McpServerE2eTest
{
    [E2eFact(DisplayName = "The server offers two catalogue tools")]
    public async Task TheServerIntroducesItselfAndOffersTheTwoCatalogueTools()
    {
        await using var assistant = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(ShopUnderTest.BaseUrl + "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        }));

        var tools = (await assistant.ListToolsAsync()).ToDictionary(tool => tool.Name);

        Assert.Equal("dca-ecommerce-sample-mcp-server", assistant.ServerInfo.Name);
        Assert.Equal("1.0.0", assistant.ServerInfo.Version);
        Assert.Equal(
            "Get all products in the catalog. Returns complete product information including SKU, name, price, category, and available stock.",
            tools["all-products"].Description);
        Assert.Equal(
            "Get detailed product information by product ID. Requires the internal product UUID. Returns complete product details including all attributes.",
            tools["product-by-id"].Description);
        Assert.Equal(["id"], Parameters(tools["product-by-id"]));
    }

    [E2eFact(DisplayName = "All products lists the range with price and stock")]
    public async Task AllProductsListsTheWholeRangeInNameOrderWithPriceAndStock()
    {
        await using var assistant = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(ShopUnderTest.BaseUrl + "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        }));

        var result = await assistant.CallToolAsync("all-products");

        using var answer = JsonDocument.Parse(Text(result));
        var products = answer.RootElement.EnumerateArray().ToList();
        var names = products.Select(product => product.GetProperty("name").GetString()!).ToList();
        Assert.Equal(21, products.Count);
        Assert.Equal(names.Order(StringComparer.Ordinal), names);
        Assert.Equal("\"Bounded Context\" Enamel Pin", names[0]);
        var book = products.Single(product => product.GetProperty("name").GetString() == "Domain-Driven Design");
        Assert.False(string.IsNullOrEmpty(book.GetProperty("id").GetString()));
        Assert.Equal("BOOK-001", book.GetProperty("sku").GetString());
        Assert.StartsWith("The seminal work by Eric Evans", book.GetProperty("description").GetString());
        Assert.False(string.IsNullOrEmpty(book.GetProperty("imageUrl").GetString()));
        Assert.Equal(54.99m, book.GetProperty("price").GetDecimal());
        Assert.Equal("EUR", book.GetProperty("currency").GetString());
        Assert.Equal("Books", book.GetProperty("category").GetString());
        Assert.Equal(20, book.GetProperty("stockQuantity").GetInt32());
        Assert.True(book.GetProperty("isAvailable").GetBoolean());
    }

    private static string Text(CallToolResult result) =>
        string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    private static IReadOnlyList<string> Parameters(McpClientTool tool) =>
        tool.JsonSchema.TryGetProperty("properties", out var properties)
            ? properties.EnumerateObject().Select(property => property.Name).ToList()
            : [];
}