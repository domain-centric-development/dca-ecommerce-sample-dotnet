using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

/// <summary>
/// The checkout pricing judges whether a line can still be bought; a price that moved is no reason to refuse — the
/// order's fingerprint says whether the customer saw it.
/// </summary>
public sealed class CheckoutPricingTest
{
    private static readonly ProductId Product = ProductId.Generate();

    [Fact]
    public void ALineWhoseCurrentPriceDiffersFromItsOwnIsNotReported()
    {
        var session = SessionWith(Money.Euro(54.99m), 1);
        var today = Facts(Money.Euro(49.99m), available: true, stock: 5);

        var validation = new CheckoutPricing().ValidateItems(session, today);

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
    }

    private static CheckoutSession SessionWith(Money unitPrice, int quantity)
    {
        var line = new CheckoutLineItem(CheckoutLineItemId.Generate(), Product, "Domain-Driven Design", unitPrice, quantity, null);
        return CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("guest"), new[] { line }, line.LineTotal);
    }

    private static IReadOnlyDictionary<ProductId, ArticlePrice> Facts(Money price, bool available, int stock) =>
        new Dictionary<ProductId, ArticlePrice> { [Product] = new ArticlePrice(price, available, stock) };
}