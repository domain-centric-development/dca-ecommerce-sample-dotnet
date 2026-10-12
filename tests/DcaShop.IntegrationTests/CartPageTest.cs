using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>The cart page a guest lands on after adding a product, through the real HTTP pipeline.</summary>
public sealed class CartPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CartPageTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheCartPageIsTitledAndHeadedShoppingCart()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var productPath = await AvailabilityPages.ProductPathAsync(client, "Domain-Driven Design");
        var productPage = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", AddForm(productPage, productPath["/products/".Length..]));

        var cartPage = await client.GetStringAsync("/cart");

        Assert.Equal("Shopping Cart", AvailabilityPages.Text(cartPage, @"<title>([^<]*)</title>"));
        Assert.Equal("Shopping Cart", AvailabilityPages.Text(cartPage, @"<h2 class=""cart__header-title"">([^<]*)</h2>"));
    }

    [Fact]
    public async Task AboveTheLinesTheCartCountsItsLinesAndItsUnits()
    {
        var client = NewBrowser();
        await AddOneUnitAsync(client, "Domain-Driven Design");
        await AddOneUnitAsync(client, "Domain-Driven Design");
        await AddOneUnitAsync(client, "Clean Architecture");

        var cartPage = await client.GetStringAsync("/cart");

        var countsAt = cartPage.IndexOf("class=\"cart__summary-row\"", StringComparison.Ordinal);
        Assert.InRange(countsAt, 0, cartPage.IndexOf("data-test=\"cart-items\"", StringComparison.Ordinal));
        var counts = cartPage[countsAt..];
        Assert.Equal("Total Items: 2", AvailabilityPages.Text(counts, @"class=""cart__summary-row""><span>([^<]*)</span>"));
        Assert.Equal("Total Quantity: 3", AvailabilityPages.Text(counts, @"class=""cart__summary-row""><span>[^<]*</span><span>([^<]*)</span>"));
    }

    [Fact]
    public async Task AnEmptyCartInvitesTheVisitorToBrowse()
    {
        var client = NewBrowser();

        var cartPage = await client.GetStringAsync("/cart");

        Assert.Equal("Your cart is empty", AvailabilityPages.Text(cartPage, @"<h3 class=""empty-state__title"">([^<]*)</h3>"));
        Assert.Equal("Browse our products to find something you'll love.", AvailabilityPages.Text(cartPage, @"<p class=""empty-state__message"">([^<]*)</p>"));
        Assert.Equal("/products", AvailabilityPages.Text(cartPage, @"<a [^>]*href=""([^""]*)""[^>]*data-test=""cart-browse-link"">Browse Products</a>"));
        Assert.DoesNotContain("data-test=\"cart-item\"", cartPage, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"cart__summary-row\"", cartPage, StringComparison.Ordinal);
        Assert.DoesNotContain("<span>Subtotal</span>", cartPage, StringComparison.Ordinal);
        Assert.DoesNotContain("data-test=\"cart-tax-row\"", cartPage, StringComparison.Ordinal);
    }

    private HttpClient NewBrowser() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task AddOneUnitAsync(HttpClient client, string productName)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(client, productName);
        var productPage = await client.GetStringAsync(productPath);
        await client.PostAsync("/cart/add-product", AddForm(productPage, productPath["/products/".Length..]));
    }

    private static FormUrlEncodedContent AddForm(string productPage, string productId)
    {
        var token = Regex.Match(productPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new([new("productId", productId), new("quantity", "1"), new("__RequestVerificationToken", token)]);
    }
}