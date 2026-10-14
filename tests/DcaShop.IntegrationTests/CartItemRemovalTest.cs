using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// "Remove" on the cart page, through the real HTTP pipeline. One client is one browser; the redirect is not followed,
/// it is an assertion.
/// </summary>
public sealed class CartItemRemovalTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CleanArchitecture = "Clean Architecture";
    private const string DomainDrivenDesign = "Domain-Driven Design";
    private const string Mug = "\"Ubiquitous Language\" Mug";
    private const string TeamTopologies = "Team Topologies";

    private readonly WebApplicationFactory<Program> _factory;

    public CartItemRemovalTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task RemovingALineTakesItsWholeQuantityOut()
    {
        var guest = CartPages.NewBrowser(_factory);
        await CartPages.AddOneUnitAsync(guest, DomainDrivenDesign);
        await CartPages.AddOneUnitAsync(guest, DomainDrivenDesign);
        await CartPages.AddOneUnitAsync(guest, DomainDrivenDesign);
        await CartPages.AddOneUnitAsync(guest, CleanArchitecture);
        var before = await guest.GetStringAsync("/cart");

        await CartPages.RemoveAsync(guest, before, CartPages.ItemIdOf(before, DomainDrivenDesign));

        var after = await guest.GetStringAsync("/cart");
        Assert.Equal([$"{CleanArchitecture} x1"], CartPages.Lines(after));
        Assert.Equal("Total Items: 1", AvailabilityPages.Text(after, @"class=""cart__summary-row""><span>([^<]*)</span>"));
        Assert.Equal("Total Quantity: 1", AvailabilityPages.Text(after, @"class=""cart__summary-row""><span>[^<]*</span><span>([^<]*)</span>"));
    }

    [Fact]
    public async Task RemovingTheLastLineLeavesAnEmptyCart()
    {
        var guest = CartPages.NewBrowser(_factory);
        await CartPages.AddOneUnitAsync(guest, DomainDrivenDesign);
        var before = await guest.GetStringAsync("/cart");

        await CartPages.RemoveAsync(guest, before, CartPages.ItemIdOf(before, DomainDrivenDesign));

        var after = await guest.GetStringAsync("/cart");
        Assert.Equal("Your cart is empty", AvailabilityPages.Text(after, @"<h3 class=""empty-state__title"">([^<]*)</h3>"));
    }

    [Fact]
    public async Task RemovingALineAnotherTabAlreadyRemovedChangesNothing()
    {
        var guest = CartPages.NewBrowser(_factory);
        await CartPages.AddOneUnitAsync(guest, DomainDrivenDesign);
        await CartPages.AddOneUnitAsync(guest, CleanArchitecture);
        var secondTab = await guest.GetStringAsync("/cart");
        var firstTab = await guest.GetStringAsync("/cart");
        var removedInTheFirstTab = await CartPages.RemoveAsync(guest, firstTab, CartPages.ItemIdOf(firstTab, CleanArchitecture));
        Assert.Equal("/cart", removedInTheFirstTab.Headers.Location?.OriginalString);

        var removedAgain = await CartPages.RemoveAsync(guest, secondTab, CartPages.ItemIdOf(secondTab, CleanArchitecture));

        Assert.Equal(HttpStatusCode.Redirect, removedAgain.StatusCode);
        Assert.Equal("/cart", removedAgain.Headers.Location?.OriginalString);
        Assert.Equal([$"{DomainDrivenDesign} x1"], CartPages.Lines(await guest.GetStringAsync("/cart")));
    }

    [Fact]
    public async Task ARemoveNamingAMalformedItemIdChangesNothing()
    {
        var guest = CartPages.NewBrowser(_factory);
        await CartPages.AddOneUnitAsync(guest, DomainDrivenDesign);
        var cartPage = await guest.GetStringAsync("/cart");

        var removed = await CartPages.RemoveAsync(guest, cartPage, "no-such-line");

        Assert.Equal(HttpStatusCode.Redirect, removed.StatusCode);
        Assert.Equal("/cart", removed.Headers.Location?.OriginalString);
        Assert.Equal([$"{DomainDrivenDesign} x1"], CartPages.Lines(await guest.GetStringAsync("/cart")));
    }

    [Fact]
    public async Task ARemoveNamingAnotherVisitorsLineChangesNeitherCart()
    {
        var first = CartPages.NewBrowser(_factory);
        await CartPages.AddOneUnitAsync(first, Mug);
        var second = CartPages.NewBrowser(_factory);
        await CartPages.AddOneUnitAsync(second, TeamTopologies);
        var mugItemId = CartPages.ItemIdOf(await first.GetStringAsync("/cart"), Mug);
        var secondPage = await second.GetStringAsync("/cart");

        var removed = await CartPages.RemoveAsync(second, secondPage, mugItemId);

        Assert.Equal(HttpStatusCode.Redirect, removed.StatusCode);
        Assert.Equal([$"{Mug} x1"], CartPages.Lines(await first.GetStringAsync("/cart")));
        Assert.Equal([$"{TeamTopologies} x1"], CartPages.Lines(await second.GetStringAsync("/cart")));
    }
}