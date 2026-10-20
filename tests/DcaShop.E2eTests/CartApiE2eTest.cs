using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DcaShop.E2eTests;

/// <summary>
/// The cart over the REST API, as a partner application meets it: no browser, a customer's Bearer token from the
/// registration. Each test registers a customer of its own, so the shared shop's other carts stay untouched.
/// </summary>
public sealed class CartApiE2eTest
{
    [E2eFact(DisplayName = "No active cart is not found")]
    public async Task ReadingTheActiveCartOfACustomerWithoutOneIsNotFound()
    {
        var token = await CustomerTokenAsync($"grace-{Guid.NewGuid():N}@example.com");
        var grace = Staff.Client(ShopUnderTest.BaseUrl, token);

        var response = await grace.GetAsync($"/api/carts/customer/{SubjectOf(token)}/active");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [E2eFact(DisplayName = "A malformed product id is a bad request")]
    public async Task AProductIdThatIsNotAUuidIsABadRequestAndLeavesTheCartEmpty()
    {
        var ada = Staff.Client(ShopUnderTest.BaseUrl, await CustomerTokenAsync($"ada-{Guid.NewGuid():N}@example.com"));
        var opened = await ada.PostAsync("/api/carts", new StringContent(string.Empty, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, opened.StatusCode);
        var cartId = (await opened.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("cartId").GetString();

        var response = await ada.PostAsync($"/api/carts/{cartId}/items", new StringContent(
            """{"productId":"no-such-product","quantity":1}""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
        var cart = await ada.GetFromJsonAsync<JsonElement>($"/api/carts/{cartId}");
        Assert.Equal(0, cart.GetProperty("items").GetArrayLength());
    }

    [E2eFact(DisplayName = "An unknown product is not available")]
    public async Task AProductTheCatalogueDoesNotKnowIsUnprocessableAndLeavesTheCartEmpty()
    {
        var ada = Staff.Client(ShopUnderTest.BaseUrl, await CustomerTokenAsync($"ada-{Guid.NewGuid():N}@example.com"));
        var opened = await ada.PostAsync("/api/carts", new StringContent(string.Empty, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, opened.StatusCode);
        var cartId = (await opened.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("cartId").GetString();

        var response = await ada.PostAsync($"/api/carts/{cartId}/items", new StringContent(
            """{"productId":"00000000-0000-4000-8000-000000000001","quantity":1}""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.UnprocessableContent, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Article not available", problem.GetProperty("title").GetString());
        Assert.Equal("There is no article for product 00000000-0000-4000-8000-000000000001.", problem.GetProperty("detail").GetString());
        var cart = await ada.GetFromJsonAsync<JsonElement>($"/api/carts/{cartId}");
        Assert.Equal(0, cart.GetProperty("items").GetArrayLength());
    }

    [E2eFact(DisplayName = "A quantity of zero is refused")]
    public async Task AQuantityOfZeroIsAnUnacceptableValueNamingTheQuantityAndLeavesTheCartEmpty()
    {
        var ada = Staff.Client(ShopUnderTest.BaseUrl, await CustomerTokenAsync($"ada-{Guid.NewGuid():N}@example.com"));
        var opened = await ada.PostAsync("/api/carts", new StringContent(string.Empty, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, opened.StatusCode);
        var cartId = (await opened.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("cartId").GetString();
        var domainDrivenDesign = await ProductIdAsync(ada, "Domain-Driven Design");

        var response = await ada.PostAsync($"/api/carts/{cartId}/items", new StringContent(
            $$"""{"productId":"{{domainDrivenDesign}}","quantity":0}""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unacceptable value", problem.GetProperty("title").GetString());
        var reasons = problem.GetProperty("errors").GetProperty("quantity");
        Assert.False(string.IsNullOrWhiteSpace(Assert.Single(reasons.EnumerateArray()).GetString()));
        var cart = await ada.GetFromJsonAsync<JsonElement>($"/api/carts/{cartId}");
        Assert.Equal(0, cart.GetProperty("items").GetArrayLength());
    }

    /// <summary>The id the catalogue gives the product of that name.</summary>
    private static async Task<string> ProductIdAsync(HttpClient client, string name)
    {
        var products = await client.GetFromJsonAsync<JsonElement>("/api/products");
        return products.EnumerateArray().Single(product => product.GetProperty("name").GetString() == name)
            .GetProperty("id").GetString()!;
    }

    private static async Task<string> CustomerTokenAsync(string email)
    {
        using var client = new HttpClient { BaseAddress = new Uri(ShopUnderTest.BaseUrl) };
        var registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Secret123",
            firstName = "Ada",
            lastName = "Lovelace",
            dateOfBirth = "1815-12-10",
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        return (await registered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    /// <summary>The customer a token names, its <c>sub</c>.</summary>
    private static string SubjectOf(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = JsonDocument.Parse(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return json.RootElement.GetProperty("sub").GetString()!;
    }
}