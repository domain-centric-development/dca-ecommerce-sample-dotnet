using DcaShop.Cart.Domain.Event;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Cart;

/// <summary>Merging is the aggregate's own operation: the target takes over the source's lines.</summary>
public sealed class ShoppingCartMergeTest
{
    private static readonly Price Ten = Price.Of(Money.Euro(10m));

    private readonly ShoppingCart _target = new(CartId.Generate(), CustomerId.Of("target-customer"));
    private readonly ShoppingCart _source = new(CartId.Generate(), CustomerId.Of("source-customer"));

    [Fact]
    public void MergesSameProductByCombiningQuantities()
    {
        var productId = ProductId.Generate();
        _target.AddItem(productId, Quantity.Of(2), Ten);
        _source.AddItem(productId, Quantity.Of(3), Ten);

        var mergedCount = _target.Merge(_source);

        Assert.Equal(1, mergedCount);
        Assert.Equal(1, _target.ItemCount);
        Assert.Equal(5, _target.TotalQuantity);
    }

    [Fact]
    public void KeepsTheLinesOwnPriceWhenTheSameProductArrivesAgain()
    {
        var productId = ProductId.Generate();
        _target.AddItem(productId, Quantity.Of(1), Ten);
        _source.AddItem(productId, Quantity.Of(1), Price.Of(Money.Euro(15m)));

        _target.Merge(_source);

        Assert.Equal(Money.Euro(20m), _target.CalculateTotal());
    }

    [Fact]
    public void AddsNewProductsAtThePriceTheSourceCapturedThem()
    {
        var targetProduct = ProductId.Generate();
        var sourceProduct = ProductId.Generate();
        _target.AddItem(targetProduct, Quantity.Of(1), Ten);
        _source.AddItem(sourceProduct, Quantity.Of(2), Price.Of(Money.Euro(25m)));

        var mergedCount = _target.Merge(_source);

        Assert.Equal(1, mergedCount);
        Assert.True(_target.ContainsProduct(targetProduct));
        Assert.True(_target.ContainsProduct(sourceProduct));
        Assert.Equal(Money.Euro(60m), _target.CalculateTotal());
    }

    [Fact]
    public void HandlesAMixOfSameAndDifferentProducts()
    {
        var common = ProductId.Generate();
        var targetOnly = ProductId.Generate();
        var sourceOnly = ProductId.Generate();
        _target.AddItem(common, Quantity.Of(1), Ten);
        _target.AddItem(targetOnly, Quantity.Of(1), Ten);
        _source.AddItem(common, Quantity.Of(2), Ten);
        _source.AddItem(sourceOnly, Quantity.Of(3), Ten);

        var mergedCount = _target.Merge(_source);

        Assert.Equal(2, mergedCount);
        Assert.Equal(3, _target.ItemCount);
        Assert.Equal(7, _target.TotalQuantity);
    }

    [Fact]
    public void MergingAnEmptySourceLeavesTheTargetUnchanged()
    {
        _target.AddItem(ProductId.Generate(), Quantity.Of(2), Ten);

        var mergedCount = _target.Merge(_source);

        Assert.Equal(0, mergedCount);
        Assert.Equal(1, _target.ItemCount);
        Assert.Equal(2, _target.TotalQuantity);
    }

    [Fact]
    public void MergingIntoAnEmptyTargetTakesOverEveryLine()
    {
        _source.AddItem(ProductId.Generate(), Quantity.Of(1), Ten);
        _source.AddItem(ProductId.Generate(), Quantity.Of(2), Ten);

        var mergedCount = _target.Merge(_source);

        Assert.Equal(2, mergedCount);
        Assert.Equal(2, _target.ItemCount);
        Assert.Equal(3, _target.TotalQuantity);
    }

    [Fact]
    public void MergingTwoEmptyCartsLeavesTheTargetEmpty()
    {
        Assert.Equal(0, _target.Merge(_source));
        Assert.True(_target.IsEmpty);
    }

    [Fact]
    public void ACheckedOutTargetRefusesTheMerge()
    {
        var productId = ProductId.Generate();
        _target.AddItem(productId, Quantity.Of(1), Ten);
        _target.Checkout();
        _source.AddItem(productId, Quantity.Of(1), Ten);

        Assert.Throws<InvalidOperationException>(() => _target.Merge(_source));
    }

    [Fact]
    public void TheSourceCartIsLeftUntouched()
    {
        _source.AddItem(ProductId.Generate(), Quantity.Of(3), Ten);

        _target.Merge(_source);

        Assert.Equal(1, _source.ItemCount);
        Assert.Equal(3, _source.TotalQuantity);
    }

    [Fact]
    public void EveryMergedLineRaisesCartItemAddedToCart()
    {
        _source.AddItem(ProductId.Generate(), Quantity.Of(1), Ten);
        _source.AddItem(ProductId.Generate(), Quantity.Of(2), Ten);
        _target.ClearDomainEvents();

        _target.Merge(_source);

        Assert.Equal(2, _target.DomainEvents.Count);
        Assert.All(_target.DomainEvents, e => Assert.IsType<CartItemAddedToCart>(e));
    }
}
