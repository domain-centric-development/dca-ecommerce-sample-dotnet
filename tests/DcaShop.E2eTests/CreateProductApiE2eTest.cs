using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DcaShop.E2eTests;

/// <summary>
/// A member of staff creating a product over the REST API, as an operator's tool meets it: no browser, a Bearer
/// token carrying the staff role. The product goes into a shop of the test's own, so the shared shop keeps the
/// seeded catalogue the other suites count.
/// </summary>
public sealed class CreateProductApiE2eTest : IDisposable
{
    private readonly SideShop _shop = SideShop.Start();

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

    private HttpClient Bearer(string token) => Staff.Client(_shop.BaseUrl, token);

    private Task<string> StaffTokenAsync(string email) => Staff.TokenAsync(_shop.BaseUrl, email);
}