using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Cart;

/// <summary>What a cart line says about its price: added at one price, priced today at another.</summary>
public sealed class EnrichedCartItemTest
{
    private static readonly Price AddedAt = Price.Of(Money.Euro(54.99m));

    internal static EnrichedCartItem LinePricedTodayAt(decimal today)
    {
        var productId = ProductId.Generate();
        return new EnrichedCartItem(CartItemId.Generate(), productId, Quantity.Of(1), AddedAt,
            new CartArticle(productId, "Domain-Driven Design", Money.Euro(today), 10, true, ""));
    }

    [Fact]
    public void ALinePricedTodayOtherThanWhenAddedHasAChangedPrice()
    {
        Assert.True(LinePricedTodayAt(49.99m).HasPriceChanged);
        Assert.False(LinePricedTodayAt(54.99m).HasPriceChanged);
    }

    [Fact]
    public void ThePriceDifferenceIsTheAbsoluteAmountBetweenTheTwoPrices() =>
        Assert.Equal(Money.Euro(5.00m), LinePricedTodayAt(49.99m).PriceDifference);

    [Fact]
    public void APriceBelowThePriceAtAdditionIsNoIncrease()
    {
        Assert.False(LinePricedTodayAt(49.99m).PriceIncreased);
        Assert.True(LinePricedTodayAt(59.99m).PriceIncreased);
    }
}