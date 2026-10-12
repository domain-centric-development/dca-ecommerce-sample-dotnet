using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The amounts below the cart's lines, read from the cart page of the wired application: the subtotal at today's
/// prices and the VAT it contains at 19 %, rounded to the cent half-up.
/// </summary>
public sealed class CartTotalsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CartTotalsTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TheSubtotalAddsUpTheLineTotals()
    {
        var client = NewBrowser();
        await AddOneUnitAsync(client, "Domain-Driven Design");
        await AddOneUnitAsync(client, "Domain-Driven Design");
        await AddOneUnitAsync(client, "Clean Architecture");

        var cartPage = await client.GetStringAsync("/cart");

        Assert.Equal("149.97 EUR", SubtotalRow(cartPage));
    }

    [Fact]
    public async Task TheVatContainedInTheSubtotalIsShownAtNineteenPercent()
    {
        var client = NewBrowser();
        await AddOneUnitAsync(client, "Domain-Driven Design");
        await AddOneUnitAsync(client, "Domain-Driven Design");
        await AddOneUnitAsync(client, "Clean Architecture");

        var cartPage = await client.GetStringAsync("/cart");

        Assert.Equal("23.94 EUR", VatRow(cartPage));
    }

    [Fact]
    public async Task TheContainedVatIsRoundedHalfUpToTheCent()
    {
        var client = NewBrowser();
        await AddOneUnitAsync(client, "Domain-Driven Design");

        var cartPage = await client.GetStringAsync("/cart");

        Assert.Equal("54.99 EUR", SubtotalRow(cartPage));
        Assert.Equal("8.78 EUR", VatRow(cartPage));
    }

    private static string SubtotalRow(string cartPage) =>
        AvailabilityPages.Text(cartPage, @"<div class=""cart__total-row""><span>Subtotal</span><span>([^<]*)</span>");

    private static string VatRow(string cartPage) =>
        AvailabilityPages.Text(cartPage, @"data-test=""cart-tax-row""><span>incl. VAT</span><span>([^<]*)</span>");

    private HttpClient NewBrowser() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task AddOneUnitAsync(HttpClient client, string productName)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(client, productName);
        var productPage = await client.GetStringAsync(productPath);
        var token = Regex.Match(productPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        await client.PostAsync("/cart/add-product",
            new FormUrlEncodedContent([new("productId", productPath["/products/".Length..]), new("quantity", "1"), new("__RequestVerificationToken", token)]));
    }
}