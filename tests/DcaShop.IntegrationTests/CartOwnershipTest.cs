using System.Net;
using System.Text.RegularExpressions;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A cart id says which cart, never whose. These are the routes where the id arrives from the browser — a path
/// segment or a hidden form field — and the caller could name somebody else's cart.
/// </summary>
/// <remarks>
/// The rule is enforced by the use cases, not by the adapters, which is why the web POST below is covered at all:
/// no amount of discipline in the REST resource would have reached it.
/// </remarks>
public sealed class CartOwnershipTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CartOwnershipTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task AStrangerCannotStartACheckoutOnSomebodyElsesCart()
    {
        var (victim, victimCartId) = await BrowserWithACartAsync();

        // A second browser that knows the id and nothing else. An empty cart page renders no form, so the
        // stranger takes their antiforgery token from a page that does.
        var stranger = NewBrowser();
        var strangerPage = await stranger.GetStringAsync(await AnyProductPathAsync(stranger));

        var started = await stranger.PostAsync("/checkout/start", Form(strangerPage, ("cartId", victimCartId)));

        // Refused, and refused as "no such cart" — the stranger learns nothing about the id
        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);
        Assert.Equal("/cart", started.Headers.Location!.ToString());

        // and no session was created on the victim's cart
        using var scope = _factory.Services.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<ICheckoutSessionRepository>();
        Assert.Null(await sessions.FindActiveByCartIdAsync(new CartId(Guid.Parse(victimCartId))));

        // The victim can still start their own checkout
        var victimPage = await victim.GetStringAsync("/cart");
        var ownStart = await victim.PostAsync("/checkout/start", Form(victimPage, ("cartId", victimCartId)));
        Assert.Equal("/checkout/buyer", ownStart.Headers.Location!.ToString());
    }

    [Fact]
    public async Task AStrangersCartPageNeverShowsSomebodyElsesCart()
    {
        var (_, victimCartId) = await BrowserWithACartAsync();

        var stranger = NewBrowser();

        Assert.DoesNotContain(victimCartId, await stranger.GetStringAsync("/cart"), StringComparison.Ordinal);
    }

    private HttpClient NewBrowser() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task<string> AnyProductPathAsync(HttpClient client)
    {
        var catalog = await client.GetStringAsync("/products");
        var path = Regex.Match(catalog, @"href=""(/products/[0-9a-f-]{36})""").Groups[1].Value;
        Assert.NotEmpty(path);
        return path;
    }

    private async Task<(HttpClient Client, string CartId)> BrowserWithACartAsync()
    {
        var client = NewBrowser();
        var productPath = await AnyProductPathAsync(client);
        var productId = productPath["/products/".Length..];
        var detail = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", Form(detail, ("productId", productId), ("quantity", "1")));

        var cartPage = await client.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        Assert.NotEmpty(cartId);
        return (client, cartId);
    }

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}
