using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

/// <summary>
/// The open checkout at today's prices — what the review shows — and the order placed exactly as the review showed it.
/// </summary>
public sealed class CheckoutSessionTodaysPricesTest
{
    private static readonly ShippingOption Standard = new("standard", "Standard Shipping", "5-7 business days", Money.Euro(4.99m));
    private static readonly ShippingOption Express = new("express", "Express Shipping", "2-3 business days", Money.Euro(9.99m));
    private static readonly ProductId DomainDrivenDesign = ProductId.Generate();
    private static readonly ProductId CleanArchitecture = ProductId.Generate();
    private static readonly ProductId Hexagon = ProductId.Generate();

    [Fact]
    public void ALineWhoseArticleIsAvailableTakesTheCurrentPrice()
    {
        var session = AtTheReview(Line(DomainDrivenDesign, 54.99m, 1));

        var lines = session.LinesAt(Today((DomainDrivenDesign, 49.99m, true)));

        Assert.Equal(Money.Euro(49.99m), Assert.Single(lines).UnitPrice);
    }

    [Fact]
    public void ALineWhoseArticleIsUnavailableOrUnknownKeepsItsPrice()
    {
        var session = AtTheReview(Line(CleanArchitecture, 39.99m, 1), Line(Hexagon, 24.99m, 1));

        var lines = session.LinesAt(Today((CleanArchitecture, 29.99m, false)));

        Assert.Equal(new[] { Money.Euro(39.99m), Money.Euro(24.99m) }, lines.Select(line => line.UnitPrice));
    }

    [Fact]
    public void TheTotalsAtTodaysPricesSumTheLinesAndKeepTheShipping()
    {
        var session = AtTheReview(Line(DomainDrivenDesign, 54.99m, 1));

        var totals = session.TotalsAt(Today((DomainDrivenDesign, 49.99m, true)));

        Assert.Equal(Money.Euro(49.99m), totals.Subtotal);
        Assert.Equal(Money.Euro(4.99m), totals.Shipping);
        Assert.Equal(Money.Euro(54.98m), totals.Total);
    }

    [Fact]
    public void ConfirmingAsReviewedStoresTheLinesAtTodaysPricesAndTheirTotal()
    {
        var session = AtTheReview(Line(DomainDrivenDesign, 10.00m, 2));
        var today = Today((DomainDrivenDesign, 11.00m, true));

        session.Confirm(new CheckoutPricing().ValidateItems(session, today), today, session.FingerprintAt(today), Authorized(session, today));

        Assert.Equal(CheckoutSessionStatus.Confirmed, session.Status);
        Assert.Equal(Money.Euro(11.00m), Assert.Single(session.LineItems).UnitPrice);
        Assert.Equal(Money.Euro(22.00m), session.Totals.Subtotal);
        Assert.Equal(Money.Euro(26.99m), session.Totals.Total);
    }

    [Fact]
    public void AFingerprintTakenAtAnotherPriceRefusesNamingTheProductAndChangesNothing()
    {
        var session = AtTheReview(Line(DomainDrivenDesign, 54.99m, 1, "Domain-Driven Design"));
        var shown = session.FingerprintAt(Today((DomainDrivenDesign, 54.99m, true)));
        var today = Today((DomainDrivenDesign, 49.99m, true));

        var refusal = Assert.Throws<PriceChangedSinceReviewException>(() =>
            session.Confirm(new CheckoutPricing().ValidateItems(session, today), today, shown, Authorized(session, today)));

        Assert.Equal("Domain-Driven Design", refusal.ProductName);
        Assert.Equal(CheckoutSessionStatus.Active, session.Status);
        Assert.Equal(Money.Euro(54.99m), Assert.Single(session.LineItems).UnitPrice);
        Assert.Equal(Money.Euro(59.98m), session.Totals.Total);
    }

    [Fact]
    public void AFingerprintOtherwiseDifferentRefusesAndChangesNothing()
    {
        var session = AtTheReview(Line(DomainDrivenDesign, 54.99m, 1));
        var today = Today((DomainDrivenDesign, 54.99m, true));
        var shown = session.FingerprintAt(today);
        session.SubmitDelivery(new DeliveryAddress("Hauptstrasse 1", "Berlin", "10115", "Germany"), Express);
        var totals = session.Totals;

        Assert.Throws<OrderChangedSinceReviewException>(() =>
            session.Confirm(new CheckoutPricing().ValidateItems(session, today), today, shown, Authorized(session, today)));

        Assert.Equal(CheckoutSessionStatus.Active, session.Status);
        Assert.Equal(totals, session.Totals);
        Assert.Equal(Express, session.ShippingOption);
    }

    [Fact]
    public void ALineAtANewPriceKeepsEverythingElse()
    {
        var line = new CheckoutLineItem(CheckoutLineItemId.Generate(), DomainDrivenDesign, "Domain-Driven Design", Money.Euro(54.99m), 2, "/images/ddd.png", "position-1");

        var repriced = line.WithUnitPrice(Money.Euro(49.99m));

        Assert.Equal(Money.Euro(49.99m), repriced.UnitPrice);
        Assert.Equal(
            (line.Id, line.ProductId, line.ProductName, line.Quantity, line.ImageUrl, line.PositionSnapshot),
            (repriced.Id, repriced.ProductId, repriced.ProductName, repriced.Quantity, repriced.ImageUrl, repriced.PositionSnapshot));
    }

    [Fact]
    public void ALineRefusesAMissingPrice()
    {
        var line = Line(DomainDrivenDesign, 54.99m, 1);

        Assert.Throws<ArgumentNullException>(() => line.WithUnitPrice(null!));
    }

    private static CheckoutLineItem Line(ProductId product, decimal unitPrice, int quantity, string name = "Article") =>
        new(CheckoutLineItemId.Generate(), product, name, Money.Euro(unitPrice), quantity, null);

    private static CheckoutSession AtTheReview(params CheckoutLineItem[] lines)
    {
        var subtotal = lines.Skip(1).Aggregate(lines[0].LineTotal, (sum, line) => sum.Add(line.LineTotal));
        var session = CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("guest"), lines, subtotal);
        session.SubmitBuyerInfo(new BuyerInfo("jane@example.com", "Jane", "Doe", "+49 30 1234567"));
        session.SubmitDelivery(new DeliveryAddress("Hauptstrasse 1", "Berlin", "10115", "Germany"), Standard);
        session.SubmitPayment(new PaymentSelection(PaymentProviderId.Of("mock")));
        return session;
    }

    /// <summary>The payment the provider authorized at "Place Order": today's total, through the provider chosen.</summary>
    private static PaymentSelection Authorized(CheckoutSession session, IReadOnlyDictionary<ProductId, ArticlePrice> today) =>
        new(session.PaymentSelection!.ProviderId, "pay-1", session.TotalsAt(today).Total);

    private static IReadOnlyDictionary<ProductId, ArticlePrice> Today(params (ProductId Product, decimal Price, bool Available)[] articles) =>
        articles.ToDictionary(a => a.Product, a => new ArticlePrice(Money.Euro(a.Price), a.Available, 10));
}