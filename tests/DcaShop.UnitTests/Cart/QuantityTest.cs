using DcaShop.Cart.Domain.Model;

namespace DcaShop.UnitTests.Cart;

public sealed class QuantityTest
{
    [Fact]
    public void SubtractLeavesTheRest() => Assert.Equal(Quantity.Of(2), Quantity.Of(5).Subtract(Quantity.Of(3)));

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void SubtractRefusesToLeaveNothing(int taken) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Quantity.Of(3).Subtract(Quantity.Of(taken)));
}