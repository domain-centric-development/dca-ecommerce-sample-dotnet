using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

/// <summary>Which line a refused order names when the stock no longer covers it.</summary>
public sealed class CheckoutStockShortageTest
{
    [Fact]
    public void AnUnavailableAndAShortLineAreNamedByTheirProduct()
    {
        var gone = new CheckoutLineItem(CheckoutLineItemId.Generate(), ProductId.Generate(), "Wooden Hexagon Desk Model", Money.Euro(10m), 1, null);
        var shortLine = new CheckoutLineItem(CheckoutLineItemId.Generate(), ProductId.Generate(), "Domain-Driven Design", Money.Euro(20m), 5, null);
        var session = CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("guest"), new[] { gone, shortLine }, Money.Euro(110m));
        var facts = new Dictionary<ProductId, ArticlePrice>
        {
            [gone.ProductId] = new(Money.Euro(10m), false, 0),
            [shortLine.ProductId] = new(Money.Euro(20m), true, 3),
        };

        var validation = new CheckoutPricing().ValidateItems(session, facts);

        Assert.Equal(
            ["Wooden Hexagon Desk Model", "Domain-Driven Design"],
            validation.Errors.Select(error => error.ProductName).ToArray());
    }

    [Fact]
    public void TheFirstLineTheStockNoLongerCoversIsTheOneNamed()
    {
        var repriced = new ValidationError(ProductId.Generate(), ErrorType.PriceChanged, "price changed", "Clean Architecture");
        var shortLine = new ValidationError(ProductId.Generate(), ErrorType.InsufficientStock, "short", "Wooden Hexagon Desk Model");
        var gone = new ValidationError(ProductId.Generate(), ErrorType.ProductUnavailable, "gone", "Domain-Driven Design");

        var validation = CheckoutValidationResult.WithErrors([repriced, shortLine, gone]);

        Assert.Equal(shortLine, validation.ShortOfStock);
    }

    [Fact]
    public void ChangedPricesAloneNameNoLine()
    {
        var repriced = new ValidationError(ProductId.Generate(), ErrorType.PriceChanged, "price changed", "Clean Architecture");

        var validation = CheckoutValidationResult.WithErrors([repriced]);

        Assert.Null(validation.ShortOfStock);
    }
}