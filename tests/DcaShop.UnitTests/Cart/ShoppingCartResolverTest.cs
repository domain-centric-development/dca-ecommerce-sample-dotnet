using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Cart;

/// <summary>What the cart owes and whether it may be settled is decided against current figures, not stored ones.</summary>
public sealed class ShoppingCartResolverTest
{
    private static readonly Price Ten = Price.Of(Money.Euro(10m));

    private readonly ShoppingCart _cart = new(CartId.Generate(), CustomerId.Of("test-customer"));
    private readonly StubPriceResolver _resolver = new();

    [Fact]
    public void AnEmptyCartOwesNothing() => Assert.Equal(Money.Euro(0m), _cart.CalculateTotal(_resolver));

    [Fact]
    public void TheTotalFollowsTheResolvedPricesNotThePricesAtAddition()
    {
        var product1 = ProductId.Generate();
        var product2 = ProductId.Generate();
        _cart.AddItem(product1, Quantity.Of(2), Ten);
        _cart.AddItem(product2, Quantity.Of(3), Ten);
        _resolver.Set(product1, Money.Euro(15m), true, 100);
        _resolver.Set(product2, Money.Euro(25m), true, 100);

        Assert.Equal(Money.Euro(105m), _cart.CalculateTotal(_resolver));
    }

    [Fact]
    public void WithoutAResolverNoTotalCanBeStated() =>
        Assert.Throws<ArgumentNullException>(() => _cart.CalculateTotal(null!));

    [Fact]
    public void AnEmptyCartHasNothingToObjectTo()
    {
        var outcome = _cart.ValidateForCheckout(_resolver);

        Assert.True(outcome.IsValid);
        Assert.Empty(outcome.Errors);
    }

    [Fact]
    public void AvailableArticlesWithEnoughStockPass()
    {
        var product1 = ProductId.Generate();
        var product2 = ProductId.Generate();
        _cart.AddItem(product1, Quantity.Of(2), Ten);
        _cart.AddItem(product2, Quantity.Of(3), Ten);
        _resolver.Set(product1, Money.Euro(10m), true, 10);
        _resolver.Set(product2, Money.Euro(10m), true, 10);

        Assert.True(_cart.ValidateForCheckout(_resolver).IsValid);
    }

    [Fact]
    public void AnArticleThatIsGoneIsNamed()
    {
        var productId = ProductId.Generate();
        _cart.AddItem(productId, Quantity.Of(1), Ten);
        _resolver.Set(productId, Money.Euro(10m), false, 0);

        var outcome = _cart.ValidateForCheckout(_resolver);

        Assert.False(outcome.IsValid);
        var error = Assert.Single(outcome.Errors);
        Assert.Equal(ValidationErrorType.ProductUnavailable, error.Type);
        Assert.Equal(productId, error.ProductId);
    }

    [Fact]
    public void StockThatDoesNotCoverTheQuantityIsNamed()
    {
        var productId = ProductId.Generate();
        _cart.AddItem(productId, Quantity.Of(5), Ten);
        _resolver.Set(productId, Money.Euro(10m), true, 3);

        var outcome = _cart.ValidateForCheckout(_resolver);

        Assert.False(outcome.IsValid);
        var error = Assert.Single(outcome.Errors);
        Assert.Equal(ValidationErrorType.InsufficientStock, error.Type);
        Assert.Equal(productId, error.ProductId);
    }

    [Fact]
    public void EveryOffendingLineIsReportedNotJustTheFirst()
    {
        var unavailable = ProductId.Generate();
        var lowStock = ProductId.Generate();
        _cart.AddItem(unavailable, Quantity.Of(1), Ten);
        _cart.AddItem(lowStock, Quantity.Of(10), Ten);
        _resolver.Set(unavailable, Money.Euro(10m), false, 0);
        _resolver.Set(lowStock, Money.Euro(10m), true, 5);

        Assert.Equal(2, _cart.ValidateForCheckout(_resolver).Errors.Count);
    }

    [Fact]
    public void StockThatExactlyCoversTheQuantityPasses()
    {
        var productId = ProductId.Generate();
        _cart.AddItem(productId, Quantity.Of(5), Ten);
        _resolver.Set(productId, Money.Euro(10m), true, 5);

        Assert.True(_cart.ValidateForCheckout(_resolver).IsValid);
    }

    [Fact]
    public void WithoutAResolverNothingCanBeValidated() =>
        Assert.Throws<ArgumentNullException>(() => _cart.ValidateForCheckout(null!));

    private sealed class StubPriceResolver : IArticlePriceResolver
    {
        private readonly Dictionary<ProductId, ArticlePrice> _prices = new();

        internal void Set(ProductId productId, Money price, bool available, int stock) =>
            _prices[productId] = new ArticlePrice(price, available, stock);

        public ArticlePrice Resolve(ProductId productId) =>
            _prices.TryGetValue(productId, out var price) ? price : new ArticlePrice(Money.Euro(0m), true, 100);
    }
}
