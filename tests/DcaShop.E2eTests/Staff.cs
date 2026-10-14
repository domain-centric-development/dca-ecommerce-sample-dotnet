using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using DcaShop.Account.Adapter.Incoming.Security;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DcaShop.E2eTests;

/// <summary>
/// A member of staff as an operator's tool meets the shop: a Bearer token carrying the staff role, and the products
/// that token creates over the REST API. The seeded catalogue and its stock are shared by every test of the suite, so a
/// test that uses up stock buys a product it created itself.
/// </summary>
public static class Staff
{
    /// <summary>
    /// A staff token for a freshly registered account of the shop at <paramref name="shopUrl"/>. No registration path
    /// hands the role out, so the token is minted the way an operator provisioning tool would, with the secret the
    /// shop is started with.
    /// </summary>
    public static async Task<string> TokenAsync(string shopUrl, string email)
    {
        using var client = new HttpClient { BaseAddress = new Uri(shopUrl) };
        var registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123",
            firstName = "Ada",
            lastName = "Lovelace",
            dateOfBirth = "1815-12-10",
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var token = (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;

        var tokens = new JwtTokenService(
            Options.Create(new JwtOptions { Secret = JwtOptions.DevelopmentSecret }),
            NullLogger<JwtTokenService>.Instance);
        return tokens.GenerateRegisteredToken(
            UserId.Of(SubjectOf(token)),
            email,
            new HashSet<Role> { Role.Customer, Role.Staff });
    }

    /// <summary>A client of the shop at <paramref name="shopUrl"/> that sends <paramref name="token"/> as Bearer token.</summary>
    public static HttpClient Client(string shopUrl, string token)
    {
        var client = new HttpClient { BaseAddress = new Uri(shopUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// A product of the test's own in the suite's shop, with <paramref name="stock"/> units, once the shop sells it: a
    /// created product is offered as unavailable until Pricing and Inventory have taken its price and stock over. Its
    /// name starts with <paramref name="name"/> and carries a suffix of its own, so it sorts after the seeded products
    /// and never meets another test's.
    /// </summary>
    public static Task<OwnProduct> CreateProductAsync(string name, decimal price, int stock) =>
        CreateProductAsync(ShopUnderTest.BaseUrl, name, price, stock);

    /// <summary>The same as <see cref="CreateProductAsync(string, decimal, int)"/>, in the shop at <paramref name="shopUrl"/>.</summary>
    public static async Task<OwnProduct> CreateProductAsync(string shopUrl, string name, decimal price, int stock)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        using var staff = Client(shopUrl, await TokenAsync(shopUrl, $"product-maker-{suffix}@example.com"));
        var response = await staff.PostAsJsonAsync("/api/products", new
        {
            sku = $"E2E-{suffix}",
            name = $"{name} {suffix}",
            description = "A product a browser test created for itself.",
            imageUrl = "/images/products/hexagon-stickers.webp",
            price,
            category = "Desk & Office",
            stock,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var product = new OwnProduct(created.GetProperty("id").GetString()!, created.GetProperty("name").GetString()!);

        await OnSaleAsync(staff, product, stock);
        return product;
    }

    /// <summary>Asks for the product again until it carries a price and <paramref name="stock"/> units, at most 10 seconds.</summary>
    private static async Task OnSaleAsync(HttpClient staff, OwnProduct product, int stock)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            var shown = await staff.GetFromJsonAsync<JsonElement>("/api/products/" + product.Id);
            if (shown.GetProperty("isAvailable").ValueKind == JsonValueKind.True
                && shown.GetProperty("price").ValueKind == JsonValueKind.Number && shown.GetProperty("price").GetDecimal() > 0
                && shown.GetProperty("stockQuantity").ValueKind == JsonValueKind.Number && shown.GetProperty("stockQuantity").GetInt32() == stock)
            {
                return;
            }

            Assert.True(DateTime.UtcNow < deadline, $"{product.Name} is not on sale with {stock} units within 10 seconds: {shown}");
            await Task.Delay(100);
        }
    }

    private static string SubjectOf(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return json.RootElement.GetProperty("sub").GetString()!;
    }
}

/// <summary>A product a test created for itself: its id and the name the shop shows.</summary>
public sealed record OwnProduct(string Id, string Name)
{
    /// <summary>The path of its product page.</summary>
    public string Path => "/products/" + Id;
}