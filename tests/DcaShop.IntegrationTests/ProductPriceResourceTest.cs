using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using DcaShop.Pricing.Application.ChangePrice;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The price resource's translation of a change: the command it makes of the address and the body, and how it answers
/// each refusal. The use case behind it is a stand-in that records what it was asked and answers as told.
/// </summary>
public sealed class ProductPriceResourceTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly RecordingChangePrice _changePrice = new();

    public ProductPriceResourceTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(s => s.AddSingleton<IChangePriceInputPort>(_changePrice)));
    }

    [Fact]
    public async Task AnAcceptedChangeAsksForTheNewPriceInEuros()
    {
        var productId = Guid.NewGuid();
        var staff = await _factory.StaffClientAsync("price-keeper@example.com");

        var response = await staff.PutAsync($"/api/products/{productId}/price", Json("""{"price":49.99}"""));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(new ChangePriceCommand(productId, 49.99m, "EUR"), Assert.Single(_changePrice.Commands));
    }

    [Fact]
    public async Task AProductWithoutAPriceIsNotFound()
    {
        var unknown = Guid.Parse("00000000-0000-4000-8000-000000000001");
        _changePrice.Refusal = new ProductPriceNotFoundException(new ProductId(unknown));
        var staff = await _factory.StaffClientAsync("price-keeper@example.com");

        var response = await staff.PutAsync($"/api/products/{unknown}/price", Json("""{"price":49.99}"""));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(unknown, Assert.Single(_changePrice.Commands).ProductId);
    }

    [Fact]
    public async Task AProductIdThatIsNotAUuidIsAnUnacceptableValue()
    {
        var staff = await _factory.StaffClientAsync("price-keeper@example.com");

        var response = await staff.PutAsync("/api/products/no-such-product/price", Json("""{"price":49.99}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.Empty(_changePrice.Commands);
    }

    [Fact]
    public async Task APriceTheModelRefusesIsAnUnacceptableValue()
    {
        _changePrice.Refusal = new ArgumentException("Amount exceeds the highest money amount");
        var staff = await _factory.StaffClientAsync("price-keeper@example.com");

        var response = await staff.PutAsync($"/api/products/{Guid.NewGuid()}/price", Json("""{"price":1000000000000.00}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    /// <summary>Records every change it is asked for; answers with the refusal it was given, else with the new price.</summary>
    private sealed class RecordingChangePrice : IChangePriceInputPort
    {
        public List<ChangePriceCommand> Commands { get; } = [];

        public Exception? Refusal { get; set; }

        public Task<ChangePriceResult> ExecuteAsync(ChangePriceCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Refusal is { } refusal
                ? Task.FromException<ChangePriceResult>(refusal)
                : Task.FromResult(new ChangePriceResult(command.ProductId, command.PriceAmount, command.PriceCurrency, DateTimeOffset.UtcNow));
        }
    }
}