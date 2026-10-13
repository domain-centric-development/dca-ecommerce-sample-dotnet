using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>The cart entry in the header every page shares, through the real HTTP pipeline: where it leads and what its badge counts.</summary>
public sealed class HeaderBasketTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HeaderBasketTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CartInTheHeaderLeadsToTheCartPage()
    {
        var client = NewBrowser();
        var catalogue = await client.GetStringAsync("/products");
        var cart = Regex.Match(Header(catalogue), @"<a\b[^>]*href=""([^""]+)""[^>]*data-test=""mini-basket-trigger"">(.*?)</a>", RegexOptions.Singleline);
        Assert.True(cart.Success, "the header offers a cart entry");
        Assert.Equal("Cart", WebUtility.HtmlDecode(Regex.Replace(cart.Groups[2].Value, "<[^>]*>", string.Empty)).Trim());

        var followed = await client.GetAsync(cart.Groups[1].Value);

        Assert.Equal(HttpStatusCode.OK, followed.StatusCode);
        Assert.Equal("/cart", followed.RequestMessage!.RequestUri!.AbsolutePath);
        Assert.Equal("Shopping Cart", AvailabilityPages.Text(await followed.Content.ReadAsStringAsync(), @"<h2 class=""cart__header-title"">([^<]*)</h2>"));
    }

    [Fact]
    public async Task TheBadgeOnTheCartEntryCountsTheUnitsInTheCart()
    {
        var client = NewBrowser();
        await AddOneUnitAsync(client, "Domain-Driven Design");
        await AddOneUnitAsync(client, "Domain-Driven Design");
        await AddOneUnitAsync(client, "Clean Architecture");

        var catalogue = await client.GetStringAsync("/products");

        Assert.Equal("3", Badge(catalogue));
    }

    [Fact]
    public async Task TheBadgeOnTheCartEntryFollowsEachAdd()
    {
        var client = NewBrowser();
        await AddOneUnitAsync(client, "Domain-Driven Design");

        var added = await AddOneUnitAsync(client, "Domain-Driven Design");

        Assert.Equal("/cart", added.Headers.Location!.ToString());
        Assert.Equal("2", Badge(await client.GetStringAsync("/cart")));
    }

    private HttpClient NewBrowser() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task<HttpResponseMessage> AddOneUnitAsync(HttpClient client, string productName)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(client, productName);
        var productPage = await client.GetStringAsync(productPath);
        var token = Regex.Match(productPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return await client.PostAsync("/cart/add-product",
            new FormUrlEncodedContent([new("productId", productPath["/products/".Length..]), new("quantity", "1"), new("__RequestVerificationToken", token)]));
    }

    /// <summary>The badge on the header's cart entry.</summary>
    private static string Badge(string page) =>
        AvailabilityPages.Text(Header(page), @"(?s)data-test=""mini-basket-trigger"">.*?data-test=""mini-basket-count"">([^<]*)</span>");

    /// <summary>The site header's markup, from its opening tag to its closing one.</summary>
    private static string Header(string page) => AvailabilityPages.Section(page, "site-header").Split("</header>")[0];
}