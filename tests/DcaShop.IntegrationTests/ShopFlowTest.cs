using System.Net;
using System.Text.RegularExpressions;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Domain.Model;
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

        // Home and catalog render the shared layout
        var home = await client.GetStringAsync("/");
        Assert.Contains("data-test=\"hero\"", home, StringComparison.Ordinal);
        var catalog = await client.GetStringAsync("/products");
        Assert.Contains("data-test=\"product-grid\"", catalog, StringComparison.Ordinal);
        var productId = Regex.Match(catalog, @"href=""/products/([0-9a-f-]{36})""").Groups[1].Value;
        Assert.NotEmpty(productId);

        // Product detail → add to cart (every POST carries the antiforgery token of the page it was rendered on)
        var detail = await client.GetStringAsync($"/products/{productId}");
        Assert.Contains("data-test=\"product-add-to-cart-form\"", detail, StringComparison.Ordinal);
        var added = await client.PostAsync("/cart/add-product", Form(detail, ("productId", productId), ("quantity", "2")));
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);

        var cartPage = await client.GetStringAsync("/cart");
        Assert.Contains("data-test=\"cart-success-message\"", cartPage, StringComparison.Ordinal);
        Assert.Contains("data-test=\"cart-item\"", cartPage, StringComparison.Ordinal);
        Assert.Contains("data-test=\"mini-basket-count\">2<", cartPage, StringComparison.Ordinal);
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        Assert.NotEmpty(cartId);

        // Start checkout → redirected to buyer info; skipping ahead is refused
        var started = await client.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);
        Assert.Equal("/checkout/buyer", started.Headers.Location!.ToString());
        var skipped = await client.GetAsync("/checkout/delivery");
        Assert.Equal(HttpStatusCode.Redirect, skipped.StatusCode);
        Assert.Equal("/checkout/buyer", skipped.Headers.Location!.ToString());

        await Step(client, "/checkout/buyer", "/checkout/delivery",
            ("email", "ada@example.com"), ("firstName", "Ada"), ("lastName", "Lovelace"), ("phone", "0123"));
        await Step(client, "/checkout/delivery", "/checkout/payment",
            ("street", "Analytical Engine Way 1"), ("city", "London"), ("postalCode", "12345"), ("country", "UK"), ("shippingOptionId", "express"));
        await Step(client, "/checkout/payment", "/checkout/review", ("providerId", "mock"));

        var review = await client.GetStringAsync("/checkout/review");
        Assert.Contains("Express Shipping", review, StringComparison.Ordinal);
        Assert.Contains("data-test=\"review-place-order-button\"", review, StringComparison.Ordinal);

        await Step(client, "/checkout/confirm", "/checkout/confirmation", tokenFrom: review);

        var confirmation = await client.GetStringAsync("/checkout/confirmation");
        Assert.Contains("data-test=\"confirmation-title\"", confirmation, StringComparison.Ordinal);
        Assert.Contains($"Order Reference: ", confirmation, StringComparison.Ordinal);

        // Cross-context, eventually consistent: the cart completes via CheckoutConfirmedEvent → ICartCompletionTrigger
        await Eventually(async () =>
        {
            // Read through the repository, not the Open Host Service: the assertion is about the system's own
            // state after an event, and there is no caller here whose cart this would be.
            using var scope = _factory.Services.CreateScope();
            var cart = await scope.ServiceProvider.GetRequiredService<IShoppingCartRepository>()
                .FindByIdAsync(new CartId(Guid.Parse(cartId)));
            return cart is { IsActive: false };
        });

        // A new, empty cart is handed out afterwards
        var freshCart = await client.GetStringAsync("/cart");
        Assert.Contains("data-test=\"cart-browse-link\"", freshCart, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownPageRendersThe404Page()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("error-page__code", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostWithoutAntiforgeryTokenIsRejected()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/cart/add-product",
            new FormUrlEncodedContent([new KeyValuePair<string, string>("productId", Guid.NewGuid().ToString()), new("quantity", "1")]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task Step(HttpClient client, string url, string expectedNext, params (string, string)[] fields) =>
        await Step(client, url, expectedNext, await client.GetStringAsync(url), fields);

    private static async Task Step(HttpClient client, string url, string expectedNext, string tokenFrom, params (string, string)[] fields)
    {
        var response = await client.PostAsync(url, Form(tokenFrom, fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedNext, response.Headers.Location!.ToString());
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
