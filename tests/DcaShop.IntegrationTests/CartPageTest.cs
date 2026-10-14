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
        var client = CartPages.NewBrowser(_factory);
        await CartPages.AddOneUnitAsync(client, "Domain-Driven Design");

        var cartPage = await client.GetStringAsync("/cart");

        Assert.Equal("Shopping Cart", AvailabilityPages.Text(cartPage, @"<title>([^<]*)</title>"));
        Assert.Equal("Shopping Cart", AvailabilityPages.Text(cartPage, @"<h2 class=""cart__header-title"">([^<]*)</h2>"));
    }

    [Fact]
    public async Task AboveTheLinesTheCartCountsItsLinesAndItsUnits()
    {
        var client = CartPages.NewBrowser(_factory);
        await CartPages.AddOneUnitAsync(client, "Domain-Driven Design");
        await CartPages.AddOneUnitAsync(client, "Domain-Driven Design");
        await CartPages.AddOneUnitAsync(client, "Clean Architecture");

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
        var client = CartPages.NewBrowser(_factory);

        var cartPage = await client.GetStringAsync("/cart");

        Assert.Equal("Your cart is empty", AvailabilityPages.Text(cartPage, @"<h3 class=""empty-state__title"">([^<]*)</h3>"));
        Assert.Equal("Browse our products to find something you'll love.", AvailabilityPages.Text(cartPage, @"<p class=""empty-state__message"">([^<]*)</p>"));
        Assert.Equal("/products", AvailabilityPages.Text(cartPage, @"<a [^>]*href=""([^""]*)""[^>]*data-test=""cart-browse-link"">Browse Products</a>"));
        Assert.DoesNotContain("data-test=\"cart-item\"", cartPage, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"cart__summary-row\"", cartPage, StringComparison.Ordinal);
        Assert.DoesNotContain("<span>Subtotal</span>", cartPage, StringComparison.Ordinal);
        Assert.DoesNotContain("data-test=\"cart-tax-row\"", cartPage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACartThatCanBeBoughtOffersTheCheckoutAndAWayBackToTheCatalogue()
    {
        var client = CartPages.NewBrowser(_factory);
        await CartPages.AddOneUnitAsync(client, "Domain-Driven Design");

        var cartPage = await client.GetStringAsync("/cart");

        var actions = AvailabilityPages.Section(cartPage, "cart-checkout-form");
        Assert.Equal("Proceed to Checkout", AvailabilityPages.Text(actions, @"<button [^>]*data-test=""cart-checkout-button"">([^<]*)</button>"));
        Assert.Equal("/products", AvailabilityPages.Text(actions, @"<a href=""([^""]*)"">Continue Shopping</a>"));
    }
}