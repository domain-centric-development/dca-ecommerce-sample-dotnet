using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>A guest browsing the shop and filling a cart through the pages, the way the cart tests arrange it.</summary>
internal static class CartPages
{
    /// <summary>A browser of its own: it keeps the cookies it is given and follows no redirect.</summary>
    public static HttpClient NewBrowser(WebApplicationFactory<Program> shop) =>
        shop.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    /// <summary>
    /// Presses "Add to Cart" on the product page of <paramref name="productName"/> with a quantity of one, and checks
    /// that the add was taken: it lands on the cart.
    /// </summary>
    public static async Task AddOneUnitAsync(HttpClient client, string productName)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(client, productName);
        var productPage = await client.GetStringAsync(productPath);

        var added = await client.PostAsync("/cart/add-product", AddForm(productPage, productPath["/products/".Length..]));

        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
        Assert.Equal("/cart", added.Headers.Location?.OriginalString);
    }

    private static FormUrlEncodedContent AddForm(string productPage, string productId)
    {
        var token = Regex.Match(productPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new([new("productId", productId), new("quantity", "1"), new("__RequestVerificationToken", token)]);
    }
}