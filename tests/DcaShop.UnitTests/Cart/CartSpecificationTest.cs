using DcaShop.Cart.Domain.Model;
using DcaShop.Cart.Domain.Specification;
using DcaShop.SharedKernel.Domain.Model;
using DcaShop.SharedKernel.Domain.Specification;

namespace DcaShop.UnitTests.Cart;

/// <summary>
/// Cart specifications are business rules the domain states and an adapter may translate. What the aggregate
/// cannot see itself (stock, customer preferences, timestamps) stays neutral in memory and is pushed down.
/// </summary>
public sealed class CartSpecificationTest
{
    private static readonly Price Ten = Price.Of(Money.Euro(10m));

    [Fact]
    public void ActiveCartHoldsUntilTheCartIsCheckedOut()
    {
        var cart = CartWithOneItem();
        var specification = new ActiveCart();

        Assert.True(specification.IsSatisfiedBy(cart));

        cart.Checkout();

        Assert.False(specification.IsSatisfiedBy(cart));
    }

    [Fact]
    public void HasMinTotalComparesTheCartsOwnTotal()
    {
        var cart = CartWithOneItem();

        Assert.True(new HasMinTotal(Money.Euro(10m)).IsSatisfiedBy(cart));
        Assert.False(new HasMinTotal(Money.Euro(10.01m)).IsSatisfiedBy(cart));
    }

    [Fact]
    public void AMinimumInAnotherCurrencyIsNotComparable() =>
        Assert.False(new HasMinTotal(Money.Of(1m, "USD")).IsSatisfiedBy(CartWithOneItem()));

    [Fact]
    public void WhatTheAggregateCannotSeeStaysNeutral()
    {
        var cart = CartWithOneItem();

        Assert.True(new HasAnyAvailableItem().IsSatisfiedBy(cart));
        Assert.True(new CustomerAllowsMarketing().IsSatisfiedBy(cart));
        Assert.True(new LastUpdatedBefore(DateTimeOffset.UnixEpoch).IsSatisfiedBy(cart));
    }

    [Fact]
    public void SpecificationsCompose()
    {
        var cart = CartWithOneItem();
        ICompositeSpecification<ShoppingCart> active = new ActiveCart();
        var rich = new HasMinTotal(Money.Euro(1000m));

        Assert.False(active.And(rich).IsSatisfiedBy(cart));
        Assert.True(active.Or(rich).IsSatisfiedBy(cart));
        Assert.False(active.Not().IsSatisfiedBy(cart));
    }

    [Fact]
    public void AVisitorThatKnowsTheLeavesSeesThemUntranslated()
    {
        var visitor = new NameCollectingVisitor();

        Assert.Equal("ActiveCart", new ActiveCart().Accept(visitor));
        Assert.Equal("HasMinTotal", new HasMinTotal(Money.Euro(1m)).Accept(visitor));
        Assert.Equal("HasAnyAvailableItem", new HasAnyAvailableItem().Accept(visitor));
        Assert.Equal("CustomerAllowsMarketing", new CustomerAllowsMarketing().Accept(visitor));
        Assert.Equal("LastUpdatedBefore", new LastUpdatedBefore(DateTimeOffset.UnixEpoch).Accept(visitor));
    }

    private static ShoppingCart CartWithOneItem()
    {
        var cart = new ShoppingCart(CartId.Generate(), CustomerId.Of("customer-1"));
        cart.AddItem(ProductId.Generate(), Quantity.Of(1), Ten);
        return cart;
    }

    private sealed class NameCollectingVisitor : ICartSpecificationVisitor<string>
    {
        public string Visit(ActiveCart specification) => nameof(ActiveCart);

        public string Visit(LastUpdatedBefore specification) => nameof(LastUpdatedBefore);

        public string Visit(HasMinTotal specification) => nameof(HasMinTotal);

        public string Visit(HasAnyAvailableItem specification) => nameof(HasAnyAvailableItem);

        public string Visit(CustomerAllowsMarketing specification) => nameof(CustomerAllowsMarketing);

        public string Visit(AndSpecification<ShoppingCart> specification) => nameof(AndSpecification<ShoppingCart>);

        public string Visit(OrSpecification<ShoppingCart> specification) => nameof(OrSpecification<ShoppingCart>);

        public string Visit(NotSpecification<ShoppingCart> specification) => nameof(NotSpecification<ShoppingCart>);
    }
}
