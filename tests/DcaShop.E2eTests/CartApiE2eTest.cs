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
}