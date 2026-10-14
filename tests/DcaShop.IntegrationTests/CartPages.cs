using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>A guest browsing the shop and filling a cart through the pages, the way the cart tests arrange it.</summary>
internal static class CartPages
{
    private static readonly Regex CartItem = new(
        @"data-test=""cart-item-name"">([^<]*)</div>.*?data-test=""cart-item-quantity"">([^<]*)</span>",
        RegexOptions.Compiled | RegexOptions.Singleline);

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

        var added = await client.PostAsync("/cart/add-product", Form(productPage, ("productId", productPath["/products/".Length..]), ("quantity", "1")));

        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
        Assert.Equal("/cart", added.Headers.Location?.OriginalString);
    }

    /// <summary>The cart page's lines as <c>name xquantity</c>, in page order.</summary>
    public static IReadOnlyList<string> Lines(string cartPage) =>
        CartItem.Matches(cartPage)
            .Select(m => $"{WebUtility.HtmlDecode(m.Groups[1].Value).Trim()} x{m.Groups[2].Value.Trim()}")
            .ToList();

    /// <summary>The item id the "Remove" form of <paramref name="productName"/>'s line on the cart page carries.</summary>
    public static string ItemIdOf(string cartPage, string productName)
    {
        var name = Regex.Escape(WebUtility.HtmlEncode(productName));
        var itemId = Regex.Match(
            cartPage,
            $@"data-test=""cart-item-name"">{name}</div>(?:(?!data-test=""cart-item"").)*?name=""cartItemId"" value=""([^""]+)""",
            RegexOptions.Singleline).Groups[1].Value;
        Assert.NotEmpty(itemId);
        return itemId;
    }

    /// <summary>
    /// Presses "Remove" on a line of the cart page <paramref name="renderedPage"/>, naming <paramref name="cartItemId"/>,
    /// and answers the shop's response.
    /// </summary>
    public static Task<HttpResponseMessage> RemoveAsync(HttpClient client, string renderedPage, string cartItemId) =>
        client.PostAsync("/cart/remove-item", Form(renderedPage, ("cartItemId", cartItemId)));

    /// <summary>A form as the page posts it: the fields and the page's antiforgery token.</summary>
    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}