using System.Net;
using System.Text.RegularExpressions;

using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetCartById;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Product.Application.GetAllProducts;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A guest's cart belongs to their browser: another browser has a cart of its own, and the tabs of one browser
/// share one cart.
/// </summary>
public sealed class GuestCartTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Mug = "\"Ubiquitous Language\" Mug";
    private const string TeamTopologies = "Team Topologies";

    private static readonly Regex CartItem = new(
        @"data-test=""cart-item-name"">([^<]*)</div>.*?data-test=""cart-item-quantity"">([^<]*)</span>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly WebApplicationFactory<Program> _factory;

    public GuestCartTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task AnotherBrowserOpensAnEmptyCart()
    {
        var guest = NewBrowser();
        await AddOneUnitThroughTheProductPageAsync(guest, Mug);

        var secondVisitor = NewBrowser();
        var cartPage = await secondVisitor.GetStringAsync("/cart");

        Assert.Equal("Shopping Cart", AvailabilityPages.Text(cartPage, @"<h2 class=""cart__header-title"">([^<]*)</h2>"));
        Assert.Empty(CartLines(cartPage));
    }

    [Fact]
    public async Task TheCartsOfTwoGuestsHoldOnlyWhatEachAdded()
    {
        var firstGuest = $"guest-{Guid.NewGuid()}";
        var secondGuest = $"guest-{Guid.NewGuid()}";
        await AddOneUnitAsync(firstGuest, Mug);

        await AddOneUnitAsync(secondGuest, TeamTopologies);

        Assert.Equal([$"{TeamTopologies} x1"], await LinesOfTheActiveCartAsync(secondGuest));
        Assert.Equal([$"{Mug} x1"], await LinesOfTheActiveCartAsync(firstGuest));
    }

    [Fact]
    public async Task TwoTabsOfOneBrowserFillOneCart()
    {
        var browserCookies = new CookieContainer();
        var firstTab = Tab(browserCookies);
        var secondTab = Tab(browserCookies);
        await AddOneUnitThroughTheProductPageAsync(firstTab, TeamTopologies);

        await AddOneUnitThroughTheProductPageAsync(secondTab, Mug);

        Assert.Equal([$"{TeamTopologies} x1", $"{Mug} x1"], CartLines(await firstTab.GetStringAsync("/cart")));
        Assert.Equal([$"{TeamTopologies} x1", $"{Mug} x1"], CartLines(await secondTab.GetStringAsync("/cart")));
    }

    private HttpClient NewBrowser() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    /// <summary>A tab is one more window onto the same browser: it presents the same cookies.</summary>
    private HttpClient Tab(CookieContainer browserCookies) => _factory.CreateDefaultClient(new CookieContainerHandler(browserCookies));

    private static async Task AddOneUnitThroughTheProductPageAsync(HttpClient browser, string productName)
    {
        var productPath = await AvailabilityPages.ProductPathAsync(browser, productName);
        var productPage = await browser.GetStringAsync(productPath);
        var token = Regex.Match(productPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);

        var added = await browser.PostAsync("/cart/add-product", new FormUrlEncodedContent(
            [new("productId", productPath["/products/".Length..]), new("quantity", "1"), new("__RequestVerificationToken", token)]));

        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
    }

    /// <summary>The cart page's lines as <c>name xquantity</c>, in page order.</summary>
    private static IReadOnlyList<string> CartLines(string cartPage) =>
        CartItem.Matches(cartPage)
            .Select(m => $"{WebUtility.HtmlDecode(m.Groups[1].Value).Trim()} x{m.Groups[2].Value.Trim()}")
            .ToList();

    private async Task AddOneUnitAsync(string customer, string productName)
    {
        using var scope = _factory.Services.CreateScope();
        var cart = await scope.ServiceProvider.GetRequiredService<IGetOrCreateActiveCartInputPort>()
            .ExecuteAsync(new GetOrCreateActiveCartCommand(customer));
        var products = await scope.ServiceProvider.GetRequiredService<IGetAllProductsInputPort>().ExecuteAsync(new GetAllProductsQuery());
        var product = products.Products.Single(p => p.Name.Value == productName);
        await scope.ServiceProvider.GetRequiredService<IAddItemToCartInputPort>()
            .ExecuteAsync(new AddItemToCartCommand(cart.CartId, customer, product.ProductId.Value, 1));
    }

    /// <summary>The lines of the customer's active cart as <c>name xquantity</c>, in the order the cart holds them.</summary>
    private async Task<IReadOnlyList<string>> LinesOfTheActiveCartAsync(string customer)
    {
        using var scope = _factory.Services.CreateScope();
        var cart = await scope.ServiceProvider.GetRequiredService<IGetOrCreateActiveCartInputPort>()
            .ExecuteAsync(new GetOrCreateActiveCartCommand(customer));
        var result = await scope.ServiceProvider.GetRequiredService<IGetCartByIdInputPort>()
            .ExecuteAsync(new GetCartByIdQuery(cart.CartId, customer));
        Assert.NotNull(result.Cart);
        return result.Cart.Items.Select(i => $"{i.Article.Name} x{i.Quantity.Value}").ToList();
    }
}