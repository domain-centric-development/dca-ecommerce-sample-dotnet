using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using DcaShop.Account.Adapter.Incoming.Security;
using DcaShop.Product.Application.CreateProduct;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The product resource's translation of a create: what it makes of a request that leaves fields out, and how it
/// refuses a caller without the staff role. The use case behind it is a stand-in that records what it was asked.
/// </summary>
public sealed class ProductCreationResourceTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly RecordingCreateProduct _createProduct = new();

    public ProductCreationResourceTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(s => s.AddSingleton<ICreateProductInputPort>(_createProduct)));
    }

    [Fact]
    public async Task AProductCreatedWithoutDescriptionAndImageAnswersBothAsEmpty()
    {
        var (_, staffUserId) = await RegisterAsync("tote-maker@example.com");
        var staff = Bearer(StaffToken(staffUserId, "tote-maker@example.com"));

        var response = await staff.PostAsync("/api/products", Json(
            """{"sku":"TOTE-001","name":"Bounded Context Tote","price":19.99,"category":"Apparel","stock":10}"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(string.Empty, body.GetProperty("description").GetString());
        Assert.Equal(string.Empty, body.GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task ACustomerCreatingAProductIsForbiddenWithAProblemDocument()
    {
        var (customerToken, _) = await RegisterAsync("ada@example.com");

        var response = await Bearer(customerToken).PostAsync("/api/products", Json(
            """{"sku":"MUG-003","name":"Forbidden Mug","description":"","imageUrl":"","price":9.99,"category":"Desk & Office","stock":1}"""));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Forbidden", body.GetProperty("title").GetString());
        Assert.DoesNotContain(_createProduct.Commands, command => command.Sku == "MUG-003");
    }

    private HttpClient Bearer(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Registers an account through the API and returns its token and user id.</summary>
    private async Task<(string Token, string UserId)> RegisterAsync(string email)
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
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        return (token, SubjectOf(token));
    }

    /// <summary>A staff token for an existing account, minted the way an operator provisioning tool would.</summary>
    private string StaffToken(string userId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<JwtTokenService>().GenerateRegisteredToken(
            UserId.Of(userId),
            email,
            new HashSet<Role> { Role.Customer, Role.Staff });
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static string SubjectOf(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return json.RootElement.GetProperty("sub").GetString()!;
    }

    /// <summary>Answers every create — the seeded catalogue's included — with the product it was asked for, priced as given.</summary>
    private sealed class RecordingCreateProduct : ICreateProductInputPort
    {
        public List<CreateProductCommand> Commands { get; } = [];

        public Task<CreateProductResult> ExecuteAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Task.FromResult(new CreateProductResult(Guid.NewGuid(), command.Sku, command.Name, Money.Euro(command.PriceAmount)));
        }
    }
}