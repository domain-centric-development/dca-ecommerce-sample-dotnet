using ModelContextProtocol.Client;

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

    private static IReadOnlyList<string> Parameters(McpClientTool tool) =>
        tool.JsonSchema.TryGetProperty("properties", out var properties)
            ? properties.EnumerateObject().Select(property => property.Name).ToList()
            : [];
}