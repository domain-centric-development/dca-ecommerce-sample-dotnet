using DcaShop.Inventory.Domain.Event;
using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Inventory;

/// <summary>
/// How a sale and a release meet the reservation: a sale takes the stock and what is reserved of it in one decrease, a
/// release gives back no more than is reserved. Each stock level starts with its events cleared, so only the
/// operation's own events are seen.
/// </summary>
public sealed class StockLevelSaleTest
{
    private static readonly ProductId Product = ProductId.Generate();

    [Fact]
    public void ASaleTakesTheAmountOutOfTheStockAndOutOfTheReservation()
    {
        var stockLevel = Reserved(available: 10, reserved: 3);

        stockLevel.Sell(2);

        Assert.Equal(8, stockLevel.AvailableQuantity.Value);
        Assert.Equal(1, stockLevel.ReservedQuantity.Value);
    }

    [Fact]
    public void ASaleBeyondTheReservationLeavesNothingReserved()
    {
        var stockLevel = Reserved(available: 10, reserved: 1);

        stockLevel.Sell(2);

        Assert.Equal(8, stockLevel.AvailableQuantity.Value);
        Assert.Equal(0, stockLevel.ReservedQuantity.Value);
    }

    [Fact]
    public void ASaleIsADecreaseAndNoCancelledReservation()
    {
        var stockLevel = Reserved(available: 10, reserved: 3);

        stockLevel.Sell(2);

        var decreased = Assert.IsType<StockDecreased>(Assert.Single(stockLevel.DomainEvents));
        Assert.Equal(2, decreased.RemovedQuantity.Value);
        Assert.Equal(8, decreased.AvailableQuantity.Value);
    }

    [Fact]
    public void ASaleOfMoreThanIsAvailableChangesNothing()
    {
        var stockLevel = Reserved(available: 4, reserved: 2);

        var refused = Assert.Throws<InsufficientStockException>(() => stockLevel.Sell(5));

        Assert.Equal(5, refused.Requested);
        Assert.Equal(4, refused.Available);
        Assert.Equal(4, stockLevel.AvailableQuantity.Value);
        Assert.Equal(2, stockLevel.ReservedQuantity.Value);
        Assert.Empty(stockLevel.DomainEvents);
    }

    [Fact]
    public void AReleaseGivesBackNoMoreThanIsReserved()
    {
        var stockLevel = Reserved(available: 10, reserved: 1);

        var released = stockLevel.ReleaseUpTo(3);

        Assert.Equal(1, released);
        Assert.Equal(0, stockLevel.ReservedQuantity.Value);
        Assert.Equal(10, stockLevel.AvailableQuantity.Value);
        Assert.Equal(1, Assert.IsType<StockReleased>(Assert.Single(stockLevel.DomainEvents)).ReleasedQuantity.Value);
    }

    [Fact]
    public void ReleasingWithNothingReservedChangesNothing()
    {
        var stockLevel = Reserved(available: 10, reserved: 0);

        var released = stockLevel.ReleaseUpTo(3);

        Assert.Equal(0, released);
        Assert.Equal(10, stockLevel.AvailableQuantity.Value);
        Assert.Empty(stockLevel.DomainEvents);
    }

    [Fact]
    public void ANegativeSaleOrReleaseStaysAMalformedCall()
    {
        var stockLevel = Reserved(available: 10, reserved: 3);

        Assert.Throws<ArgumentException>(() => stockLevel.Sell(-1));
        Assert.Throws<ArgumentException>(() => stockLevel.ReleaseUpTo(-1));
    }

    private static StockLevel Reserved(int available, int reserved)
    {
        var stockLevel = StockLevel.Create(Product, available);
        if (reserved > 0)
        {
            stockLevel.Reserve(reserved);
        }

        stockLevel.ClearDomainEvents();
        return stockLevel;
    }
}