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
/// A member of staff creating a product over the REST API, as an operator's tool meets it: no browser, a Bearer
/// token carrying the staff role. The product goes into a shop of the test's own, so the shared shop keeps the
/// seeded catalogue the other suites count.
/// </summary>
public sealed class CreateProductApiE2eTest : IDisposable
{
    private readonly SideShop _shop = RestartedShop.Start();

    [E2eFact(DisplayName = "Staff create a product")]
    public async Task AStaffMemberCreatesAProductAndTheAnswerCarriesItsPriceInEuros()
    {
        var staff = Bearer(await StaffTokenAsync("mug-maker@example.com"));

        var response = await staff.PostAsync("/api/products", new StringContent(
            """{"sku":"MUG-002","name":"Aggregate Mug","description":"A mug with a consistency boundary.","imageUrl":"","price":14.99,"category":"Desk & Office","stock":12}""",
            System.Text.Encoding.UTF8,
            "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("id").GetString()));
        Assert.Equal("MUG-002", body.GetProperty("sku").GetString());
        Assert.Equal("Aggregate Mug", body.GetProperty("name").GetString());
        Assert.Equal("14.99", body.GetProperty("price").GetRawText());
        Assert.Equal("EUR", body.GetProperty("currency").GetString());
        Assert.Equal("Desk & Office", body.GetProperty("category").GetString());
    }

    [E2eFact(DisplayName = "An overlong image url is refused")]
    public async Task AnImageUrlLongerThan2000CharactersIsRefusedAsAnUnacceptableValue()
    {
        var staff = Bearer(await StaffTokenAsync("big-picture@example.com"));
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

    [E2eFact(DisplayName = "A price that rounds to zero is refused")]
    public async Task APriceBelowACentIsRefusedAsAnUnacceptableValue()
    {
        var staff = Bearer(await StaffTokenAsync("tiny-mug-maker@example.com"));

        var response = await staff.PostAsync("/api/products", new StringContent(
            """{"sku":"TINY-001","name":"Tiny Mug","description":"","imageUrl":"","price":0.001,"category":"Desk & Office","stock":5}""",
            System.Text.Encoding.UTF8,
            "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    [E2eFact(DisplayName = "A duplicate sku is a conflict")]
    public async Task ASkuTheCatalogueAlreadyCarriesIsAConflictAndNoProductIsAdded()
    {
        var staff = Bearer(await StaffTokenAsync("second-book@example.com"));
        var seeded = (await staff.GetFromJsonAsync<JsonElement>("/api/products")).EnumerateArray()
            .Single(p => p.GetProperty("sku").GetString() == "BOOK-001");
        Assert.Equal("Domain-Driven Design", seeded.GetProperty("name").GetString());

        var response = await staff.PostAsync("/api/products", new StringContent(
            """{"sku":"BOOK-001","name":"Another Book","description":"","imageUrl":"","price":19.99,"category":"Books","stock":5}""",
            System.Text.Encoding.UTF8,
            "application/json"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Stock keeping unit already in use", problem.GetProperty("title").GetString());
        Assert.Equal("A product with SKU BOOK-001 already exists.", problem.GetProperty("detail").GetString());
        Assert.Equal(21, (await staff.GetFromJsonAsync<JsonElement>("/api/products")).GetArrayLength());
    }

    public void Dispose() => _shop.Dispose();

    private HttpClient Bearer(string token)
    {
        var client = new HttpClient { BaseAddress = new Uri(_shop.BaseUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// A staff token for a freshly registered account. No registration path hands the role out, so the test mints
    /// the token the way an operator provisioning tool would, with the secret the shop under test is started with.
    /// </summary>
    private async Task<string> StaffTokenAsync(string email)
    {
        using var client = new HttpClient { BaseAddress = new Uri(_shop.BaseUrl) };
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
            new HashSet<string> { IIdentityProvider.IIdentity.RoleCustomer, IIdentityProvider.IIdentity.RoleStaff });
    }

    private static string SubjectOf(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return json.RootElement.GetProperty("sub").GetString()!;
    }
}