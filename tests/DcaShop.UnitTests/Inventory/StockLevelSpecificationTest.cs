using DcaShop.Inventory.Domain.Model;
using DcaShop.Inventory.Domain.Specification;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Inventory;

/// <summary>
/// The rule behind the operator's overview: stock is short only while the quantity held stays under the
/// threshold. The boundary itself is not short, and reservations do not enter the comparison.
/// </summary>
public sealed class StockLevelSpecificationTest
{
    [Fact]
    public void HoldsWhileLessIsHeldThanTheThreshold()
    {
        var stockLevel = StockLevelOf(4);

        Assert.True(new AvailableQuantityBelow(StockQuantity.Of(5)).IsSatisfiedBy(stockLevel));
    }

    [Fact]
    public void DoesNotHoldOnceTheThresholdIsReached()
    {
        var stockLevel = StockLevelOf(4);

        Assert.False(new AvailableQuantityBelow(StockQuantity.Of(4)).IsSatisfiedBy(stockLevel));
        Assert.False(new AvailableQuantityBelow(StockQuantity.Of(3)).IsSatisfiedBy(stockLevel));
    }

    [Fact]
    public void ComparesTheQuantityHeldRegardlessOfReservations()
    {
        var stockLevel = StockLevelOf(4);
        stockLevel.Reserve(4);

        Assert.True(new AvailableQuantityBelow(StockQuantity.Of(5)).IsSatisfiedBy(stockLevel));
        Assert.False(new AvailableQuantityBelow(StockQuantity.Of(4)).IsSatisfiedBy(stockLevel));
    }

    private static StockLevel StockLevelOf(int quantity) => StockLevel.Create(ProductId.Generate(), quantity);
}
