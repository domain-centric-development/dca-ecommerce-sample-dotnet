using System.Text.Json;

using DcaShop.Product.Application.GetProductById;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The MCP tool <c>product-by-id</c> as an assistant calls it: an id is any string. What the tool makes of an id that
/// names no product and of a blank one — its translation, against an input port that knows no product — and what it
/// answers for an id <c>all-products</c> listed, against the seeded catalogue.
/// </summary>
public sealed class McpToolTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public McpToolTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnIdThatNamesNoProductAnswersNull()
    {
        var port = new NoProducts();
        await using var assistant = await AssistantAsync(port);

        var result = await CallProductByIdAsync(assistant, "no-such-product");

        Assert.NotEqual(true, result.IsError);
        Assert.True(AnswersNull(result), "expected the answer null, was: " + Text(result));
    }

    [Fact]
    public async Task ABlankIdIsAToolError()
    {
        var port = new NoProducts();
        await using var assistant = await AssistantAsync(port);

        var result = await CallProductByIdAsync(assistant, "");

        Assert.True(result.IsError);
        Assert.Equal("Product ID cannot be null or blank", Text(result));
        Assert.Empty(port.Queries);
    }

    [Fact]
    public async Task TheIdOfAListedProductAnswersThatProductWithPriceAndStock()
    {
        await using var assistant = await AssistantAsync(_factory);
        var listed = await assistant.CallToolAsync("all-products");
        using var range = JsonDocument.Parse(Text(listed));
        var id = range.RootElement.EnumerateArray()
            .Single(product => product.GetProperty("name").GetString() == "Wooden Hexagon Desk Model")
            .GetProperty("id").GetString()!;

        var result = await CallProductByIdAsync(assistant, id);

        using var answer = JsonDocument.Parse(Text(result));
        var product = answer.RootElement;
        Assert.Equal(id, product.GetProperty("id").GetString());
        Assert.Equal("HEXAGON-001", product.GetProperty("sku").GetString());
        Assert.Equal(39.99m, product.GetProperty("price").GetDecimal());
        Assert.Equal("EUR", product.GetProperty("currency").GetString());
        Assert.Equal("Desk & Office", product.GetProperty("category").GetString());
        Assert.Equal(8, product.GetProperty("stockQuantity").GetInt32());
        Assert.True(product.GetProperty("isAvailable").GetBoolean());
    }

    private async Task<McpClient> AssistantAsync(IGetProductByIdInputPort port)
    {
        var shop = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton(port)));
        return await AssistantAsync(shop);
    }

    private static async Task<McpClient> AssistantAsync(WebApplicationFactory<Program> shop)
    {
        var http = shop.CreateClient();
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            http);
        return await McpClient.CreateAsync(transport);
    }

    private static async Task<CallToolResult> CallProductByIdAsync(McpClient assistant, string id) =>
        await assistant.CallToolAsync("product-by-id", new Dictionary<string, object?> { ["id"] = id });

    private static string Text(CallToolResult result) =>
        string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    /// <summary>A null answer: no product in the content, and no structured product beside it.</summary>
    private static bool AnswersNull(CallToolResult result) =>
        (result.Content.Count == 0 || Text(result).Trim() == "null")
        && (result.StructuredContent is null || result.StructuredContent.Value.ValueKind == JsonValueKind.Null);

    /// <summary>The input port of a catalogue without products; it records what it was asked.</summary>
    private sealed class NoProducts : IGetProductByIdInputPort
    {
        public List<GetProductByIdQuery> Queries { get; } = new();

        public Task<GetProductByIdResult> ExecuteAsync(GetProductByIdQuery query, CancellationToken cancellationToken = default)
        {
            Queries.Add(query);
            return Task.FromResult(new GetProductByIdResult(null));
        }
    }
}