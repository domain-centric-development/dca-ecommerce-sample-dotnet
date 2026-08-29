using DcaShop.Cart.Domain.Event;
using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Cart;

public sealed class ShoppingCartTest
{
    private static readonly Price Ten = Price.Of(Money.Euro(10m));

    private static ShoppingCart NewCart() => new(CartId.Generate(), CustomerId.Of("guest-1"));

    [Fact]
    public void AddingSameProductTwiceIncreasesQuantityInsteadOfAddingLine()
    {
        var cart = NewCart();
        var productId = ProductId.Generate();

        cart.AddItem(productId, Quantity.Of(1), Ten);
        cart.AddItem(productId, Quantity.Of(2), Ten);

        var item = Assert.Single(cart.Items);
        Assert.Equal(3, item.Quantity.Value);
        Assert.Equal(2, cart.DomainEvents.Count);
        Assert.All(cart.DomainEvents, e => Assert.IsType<CartItemAddedToCart>(e));
    }

    [Fact]
    public void RemoveItemRaisesProductRemovedFromCart()
    {
        var cart = NewCart();
        var productId = ProductId.Generate();
        cart.AddItem(productId, Quantity.Of(1), Ten);
        cart.ClearDomainEvents();

        cart.RemoveItem(cart.Items[0].Id);

        Assert.Empty(cart.Items);
        var removed = Assert.IsType<ProductRemovedFromCart>(Assert.Single(cart.DomainEvents));
        Assert.Equal(productId, removed.ProductId);
    }

    [Fact]
    public void CheckoutLocksCartAndCarriesTotal()
    {
        var cart = NewCart();
        cart.AddItem(ProductId.Generate(), Quantity.Of(2), Ten);
        cart.ClearDomainEvents();

        cart.Checkout();

        Assert.Equal(CartStatus.CheckedOut, cart.Status);
        var checkedOut = Assert.IsType<CartCheckedOut>(Assert.Single(cart.DomainEvents));
        Assert.Equal(Money.Euro(20m), checkedOut.TotalAmount);
        Assert.Throws<InvalidOperationException>(() => cart.AddItem(ProductId.Generate(), Quantity.Of(1), Ten));
    }

    [Fact]
    public void EmptyCartCannotBeCheckedOut() =>
        Assert.Throws<InvalidOperationException>(() => NewCart().Checkout());

    [Fact]
    public void CompleteFromActiveIsAllowedButNotFromAbandoned()
    {
        var cart = NewCart();
        cart.Complete();
        Assert.Equal(CartStatus.Completed, cart.Status);

        var abandoned = NewCart();
        abandoned.Abandon();
        Assert.Throws<InvalidOperationException>(abandoned.Complete);
    }

    [Fact]
    public void ReconstituteRestoresLinesWithoutRaisingEvents()
    {
        var id = CartId.Generate();
        var stored = new ShoppingCart.StoredItem(CartItemId.Generate(), ProductId.Generate(), Quantity.Of(4), Ten);

        var cart = ShoppingCart.Reconstitute(id, CustomerId.Of("c"), CartStatus.CheckedOut, new[] { stored });

        Assert.Empty(cart.DomainEvents);
        Assert.Equal(CartStatus.CheckedOut, cart.Status);
        Assert.Equal(stored.Id, Assert.Single(cart.Items).Id);
    }

    [Fact]
    public void QuantityMustBePositive() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Quantity.Of(0));

    [Fact]
    public void EnrichedCartDetectsPriceChangesAndCheckoutEligibility()
    {
        var cart = NewCart();
        var productId = ProductId.Generate();
        cart.AddItem(productId, Quantity.Of(2), Ten);
        var articles = new Dictionary<ProductId, CartArticle>
        {
            [productId] = new(productId, "Thing", Money.Euro(12m), 1, true, ""),
        };

        var enriched = new EnrichedCartFactory().Create(cart, articles);

        Assert.True(enriched.HasAnyPriceChanges);
        Assert.Equal(Money.Euro(24m), enriched.CurrentSubtotal);
        Assert.Equal(Money.Euro(20m), enriched.OriginalSubtotal);
        Assert.False(enriched.IsValidForCheckout); // stock 1 < quantity 2
    }
}
