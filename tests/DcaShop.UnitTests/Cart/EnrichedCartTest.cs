using DcaShop.Cart.Domain.Model;

using static DcaShop.UnitTests.Cart.EnrichedCartItemTest;

namespace DcaShop.UnitTests.Cart;

/// <summary>What a cart says about the prices of its lines.</summary>
public sealed class EnrichedCartTest
{
    [Fact]
    public void ACartWithALineWhosePriceChangedHasPriceChanges()
    {
        var cart = new EnrichedCart(CartId.Generate(), CustomerId.Of("guest-1"), CartStatus.Active,
            [LinePricedTodayAt(54.99m), LinePricedTodayAt(49.99m)]);

        Assert.True(cart.HasAnyPriceChanges);
    }
}