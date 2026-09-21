using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Inventory;

/// <summary>
/// What the stock keeping unit refuses, and how it says so. A quantity the warehouse does not have is a
/// business answer with a name; a negative quantity is a malformed call and stays an argument guard, so a
/// caller can tell the two apart without reading a message.
/// </summary>
public sealed class StockLevelRefusalTest
{
    private static readonly ProductId Product = ProductId.Generate();

    [Fact]
    public void DecreasingByMoreThanIsHeldNamesTheQuantityThatWasThere()
    {
        var stockLevel = StockLevel.Create(Product, 4);

        var refused = Assert.Throws<InsufficientStockException>(() => stockLevel.DecreaseStock(5));

        Assert.Equal(Product, refused.ProductId);
        Assert.Equal(5, refused.Requested);
        Assert.Equal(4, refused.Available);
    }

    [Fact]
    public void ReservingBeyondTheUnreservedPartNamesWhatWasStillPromisable()
    {
        var stockLevel = StockLevel.Create(Product, 4);
        stockLevel.Reserve(3);

        var refused = Assert.Throws<InsufficientUnreservedStockException>(() => stockLevel.Reserve(2));

        Assert.Equal(2, refused.Requested);
        Assert.Equal(1, refused.Unreserved);
    }

    [Fact]
    public void ReleasingMoreThanWasReservedNamesTheReservation()
    {
        var stockLevel = StockLevel.Create(Product, 4);
        stockLevel.Reserve(1);

        var refused = Assert.Throws<InsufficientReservedStockException>(() => stockLevel.Release(2));

        Assert.Equal(2, refused.Requested);
        Assert.Equal(1, refused.Reserved);
    }

    [Fact]
    public void ANegativeQuantityStaysAMalformedCall()
    {
        var stockLevel = StockLevel.Create(Product, 4);

        Assert.Throws<ArgumentException>(() => stockLevel.DecreaseStock(-1));
        Assert.Throws<ArgumentException>(() => stockLevel.Reserve(-1));
        Assert.Throws<ArgumentException>(() => stockLevel.Release(-1));
    }
}
