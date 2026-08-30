using System.Net;
using System.Text.RegularExpressions;
using DcaShop.Cart.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>Browse → add to cart → checkout in five steps → confirmation, through the real HTTP pipeline.</summary>
public sealed class ShopFlowTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ShopFlowTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CustomerBuysAProduct()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        // Catalog lists seeded products
        var catalog = await client.GetStringAsync("/products");
        var productId = Regex.Match(catalog, @"name=""productId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        Assert.NotEmpty(productId);

        // Add to cart (every POST carries the antiforgery token of the page it was rendered on)
        var added = await client.PostAsync("/cart/items", Form(catalog, ("productId", productId), ("quantity", "2")));
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);

        var cartPage = await client.GetStringAsync("/cart");
        Assert.Contains("data-test=\"cart-line\"", cartPage, StringComparison.Ordinal);
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        Assert.NotEmpty(cartId);

        // Start checkout → redirected to buyer info
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        var buyerInfoUrl = started.Headers.Location!.ToString();
        var sessionId = Regex.Match(buyerInfoUrl, @"/checkout/([0-9a-f-]{36})/buyer-info").Groups[1].Value;
        Assert.NotEmpty(sessionId);

        // Skipping ahead is refused: delivery redirects back to the current step
        var skipped = await client.GetAsync($"/checkout/{sessionId}/delivery");
        Assert.Equal(HttpStatusCode.Redirect, skipped.StatusCode);

        await Step(client, $"/checkout/{sessionId}/buyer-info", "delivery",
            ("email", "ada@example.com"), ("firstName", "Ada"), ("lastName", "Lovelace"), ("phone", "0123"));
        await Step(client, $"/checkout/{sessionId}/delivery", "payment",
            ("street", "Analytical Engine Way 1"), ("city", "London"), ("postalCode", "12345"), ("country", "UK"), ("shippingOptionId", "express"));
        await Step(client, $"/checkout/{sessionId}/payment", "review", ("paymentProviderId", "invoice"));

        var review = await client.GetStringAsync($"/checkout/{sessionId}/review");
        Assert.Contains("Express Shipping", review, StringComparison.Ordinal);

        await Step(client, $"/checkout/{sessionId}/confirm", "confirmation", tokenFrom: review);

        var confirmation = await client.GetStringAsync($"/checkout/{sessionId}/confirmation");
        Assert.Contains("data-test=\"session-status\">Confirmed<", confirmation, StringComparison.Ordinal);

        // Cross-context, eventually consistent: the cart completes via CheckoutConfirmedEvent → ICartCompletionTrigger
        await Eventually(async () =>
        {
            using var scope = _factory.Services.CreateScope();
            var snapshot = await scope.ServiceProvider.GetRequiredService<CartService>().FindCartByIdAsync(Guid.Parse(cartId));
            return snapshot is { Active: false };
        });

        // A new cart is handed out afterwards
        var freshCart = await client.GetStringAsync("/cart");
        Assert.Contains("data-test=\"cart-empty\"", freshCart, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostWithoutAntiforgeryTokenIsRejected()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/cart/items",
            new FormUrlEncodedContent([new KeyValuePair<string, string>("productId", Guid.NewGuid().ToString()), new("quantity", "1")]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task Step(HttpClient client, string url, string expectedNextStep, params (string, string)[] fields) =>
        await Step(client, url, expectedNextStep, await client.GetStringAsync(url), fields);

    private static async Task Step(HttpClient client, string url, string expectedNextStep, string tokenFrom, params (string, string)[] fields)
    {
        var response = await client.PostAsync(url, Form(tokenFrom, fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("/" + expectedNextStep, response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }

    private static async Task Eventually(Func<Task<bool>> condition)
    {
        for (var i = 0; i < 50; i++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Fail("condition not met within 5 seconds");
    }
}
