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

    private static FormUrlEncodedContent AddForm(string productPage, string productId)
    {
        var token = Regex.Match(productPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new([new("productId", productId), new("quantity", "1"), new("__RequestVerificationToken", token)]);
    }
}